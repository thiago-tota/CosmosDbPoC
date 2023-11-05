using System.Diagnostics;
using System.Linq.Expressions;
using System.Net;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace CosmosDbPoC.Data.Repository;

public class CosmosRepository<T> : ICosmosRepository<T> where T : IEntity
{
    private readonly CosmosClient _client;

    private Database _database = null!;
    private Container _container = null!;

    public CosmosRepository(string host, string primaryKey, string databaseName, string containerId)
    {
        _client = new CosmosClient(host, primaryKey, new CosmosClientOptions {AllowBulkExecution = true});
        Initialize(databaseName, containerId).GetAwaiter().GetResult();
    }

    private async Task Initialize(string databaseName, string containerId)
    {
        var databaseResponse = await _client.CreateDatabaseIfNotExistsAsync(databaseName);
        _database = databaseResponse.Database;

        var containerProperties = new ContainerProperties(containerId, "/partitionKey")
        {
            IndexingPolicy = new IndexingPolicy
            {
                IndexingMode = IndexingMode.Consistent,
                ExcludedPaths = {new ExcludedPath {Path = "/*"}} // This line exclude all fields indexing to optimize the write throughput. 
            }
        };

        var containerResponse = await _database.CreateContainerIfNotExistsAsync(containerProperties, 1000);
        _container = containerResponse.Container;
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        return await GetByFieldAsync(f => true);
    }

    public async Task<T> GetByIdAsync(string id)
    {
        return (await GetByFieldAsync(f => f.Id == id)).FirstOrDefault()!;
    }

    public async Task<IEnumerable<T>> GetByFieldAsync(Expression<Func<T, bool>> predicate)
    {
        var results = new List<T>();

        var feedIterator = _container.GetItemLinqQueryable<T>().Where(predicate).ToFeedIterator();

        while (feedIterator.HasMoreResults)
        {
            var response = await feedIterator.ReadNextAsync();
            results.AddRange(response.ToList());
        }

        return results;
    }

    public async Task<bool> CreateAsync(T entity)
    {
        var response = await _container.CreateItemAsync(entity, new PartitionKey(entity.PartitionKey));
        Debug.WriteLine(response.ToJson());

        return response.StatusCode == HttpStatusCode.Created;
    }

    public async Task<IEnumerable<(bool Result, string Error)>> CreateAsync(IEnumerable<T> entities)
    {
        // IMPORTANT: With custom indexing policy to exclude all fields from being indexed the write has significant improvement.
        // It dropped from 26.5s to 16s when inserting 1.000 records with 400 of throughput.
        // Increasing the throughput to 1.000 the time dropped from 16s to 6s. It's needed to be careful with the costs.
        // However, MongoDB still showed better performance when inserting the same 1.000 records.
        // MongoDB doesn't have throughput configuration what is probably always running at as best as possible.
        
        var stopwatch = new Stopwatch();
        stopwatch.Restart();

        var tasks = entities
            .Select(entity => _container.CreateItemAsync(entity, new PartitionKey(entity.PartitionKey))
                .ContinueWith(ContinuationFunction)).ToList();

        await Task.WhenAll(tasks);

        var tasksResponse = tasks
            .Select(f => (f.Result.IsSuccess, f.Result.Error)).ToList();

        stopwatch.Stop();
        Debug.WriteLine(stopwatch.Elapsed);

        return tasksResponse;
    }

    public async Task<bool> UpdateAsync(string id, T entity)
    {
        var itemResponse = await _container.ReplaceItemAsync(entity, id);
        return itemResponse.StatusCode == HttpStatusCode.OK;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        return await DeleteAsync(entity);
    }

    public async Task<bool> DeleteAsync(T entity)
    {
        var itemResponse = await _container.DeleteItemAsync<T>(entity.Id, new PartitionKey(entity.PartitionKey));
        return itemResponse.StatusCode == HttpStatusCode.NoContent;
    }

    private static (bool IsSuccess, string Error) ContinuationFunction(Task<ItemResponse<T>> response)
    {
        return (response.IsCompletedSuccessfully, GetExceptionMessage(response.Exception?.Flatten()));
    }

    private static string GetExceptionMessage(AggregateException? aggregateException)
    {
        if (aggregateException is null)
            return string.Empty;

        if (aggregateException?.InnerExceptions.FirstOrDefault(innerEx => innerEx is CosmosException) is
            CosmosException cosmosException)
        {
            return $"Exception {cosmosException.StatusCode} ({cosmosException.Message}).";
        }

        return $"Exception {aggregateException?.InnerExceptions.FirstOrDefault()}.";
    }
}