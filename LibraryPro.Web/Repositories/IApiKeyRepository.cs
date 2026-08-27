using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories;

public interface IApiKeyRepository
{
    Task<IEnumerable<ApiKey>> GetAllAsync();
    Task<ApiKey?> GetByIdAsync(int id);
    Task<ApiKey?> GetByKeyAsync(string key);
    Task AddAsync(ApiKey apiKey);
    Task UpdateAsync(ApiKey apiKey);
    Task DeleteAsync(int id);
    Task IncrementUsageAsync(int id);
}
