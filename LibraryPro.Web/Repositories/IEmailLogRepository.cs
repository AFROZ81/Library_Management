using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface IEmailLogRepository
    {
        Task<IEnumerable<EmailLog>> GetAllAsync();
        Task<EmailLog?> GetByIdAsync(int id);
        Task<IEnumerable<EmailLog>> GetByEmailAsync(string email);
        Task<IEnumerable<EmailLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<IEnumerable<EmailLog>> GetFailedEmailsAsync();
        Task AddAsync(EmailLog emailLog);
        Task UpdateAsync(EmailLog emailLog);
        Task DeleteAsync(int id);
    }
}
