using Newtonsoft.Json;

namespace CosmosDbPoC.Data
{
    public interface IEntity
    {
        [JsonProperty(PropertyName = "id")]
        string Id { get; set; }

        [JsonProperty(PropertyName = "partitionKey")]
        string PartitionKey { get; set; }

        static string? ContainerId { get; }
    }
}
