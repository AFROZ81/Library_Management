using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApiKeyRepository> _logger;

    public ApiKeyRepository(ApplicationDbContext context, ILogger<ApiKeyRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ApiKey>> GetAllAsync()
    {
        return await _context.ApiKeys.ToListAsync();
    }

    public async Task<ApiKey?> GetByIdAsync(int id)
    {
        return await _context.ApiKeys.FindAsync(id);
    }

    public async Task<ApiKey?> GetByKeyAsync(string key)
    {
        return await _context.ApiKeys.FirstOrDefaultAsync(k => k.Key == key && k.IsActive);
    }

    public async Task AddAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ApiKey apiKey)
    {
        _context.ApiKeys.Update(apiKey);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey != null)
        {
            _context.ApiKeys.Remove(apiKey);
            await _context.SaveChangesAsync();
        }
    }

    public async Task IncrementUsageAsync(int id)
    {
        var apiKey = await _context.ApiKeys.FindAsync(id);
        if (apiKey != null)
        {
            apiKey.UsageCount++;
            apiKey.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
