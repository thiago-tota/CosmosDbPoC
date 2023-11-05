using System.Linq.Expressions;

namespace CosmosDbPoC.Data.Repository
{
    internal interface ICosmosRepository<T> where T : IEntity
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<IEnumerable<T>> GetByFieldAsync(Expression<Func<T, bool>> predicate);
        Task<bool> CreateAsync(T entity);
        Task<IEnumerable<(bool Result, string Error)>> CreateAsync(IEnumerable<T> entities);
        Task<bool> UpdateAsync(string id, T entity);
        Task<bool> DeleteAsync(string id);
        Task<bool> DeleteAsync(T entity);
    }
}