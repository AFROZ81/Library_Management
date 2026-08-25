using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface IAuditLogRepository
    {
        Task<IEnumerable<AuditLog>> GetAllAsync();
        Task<IEnumerable<AuditLog>> GetByUserIdAsync(string userId);
        Task<IEnumerable<AuditLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<AuditLog>> GetByOperationTypeAsync(string operationType);
        Task<IEnumerable<AuditLog>> GetByEntityTypeAsync(string entityType);
        Task<AuditLog?> GetByIdAsync(int id);
        Task AddAsync(AuditLog auditLog);
        Task AddRangeAsync(IEnumerable<AuditLog> auditLogs);
        Task DeleteOldLogsAsync(DateTime cutoffDate);
    }
}
