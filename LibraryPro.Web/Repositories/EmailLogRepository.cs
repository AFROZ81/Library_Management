using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class EmailLogRepository : IEmailLogRepository
    {
        private readonly ApplicationDbContext _context;

        public EmailLogRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<EmailLog>> GetAllAsync()
        {
            return await _context.EmailLogs
                .OrderByDescending(e => e.SentAt)
                .ToListAsync();
        }

        public async Task<EmailLog?> GetByIdAsync(int id)
        {
            return await _context.EmailLogs.FindAsync(id);
        }

        public async Task<IEnumerable<EmailLog>> GetByEmailAsync(string email)
        {
            return await _context.EmailLogs
                .Where(e => e.ToEmail == email)
                .OrderByDescending(e => e.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmailLog>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.EmailLogs
                .Where(e => e.SentAt >= startDate && e.SentAt <= endDate)
                .OrderByDescending(e => e.SentAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<EmailLog>> GetFailedEmailsAsync()
        {
            return await _context.EmailLogs
                .Where(e => !e.IsSuccess)
                .OrderByDescending(e => e.SentAt)
                .ToListAsync();
        }

        public async Task AddAsync(EmailLog emailLog)
        {
            await _context.EmailLogs.AddAsync(emailLog);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(EmailLog emailLog)
        {
            _context.EmailLogs.Update(emailLog);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var emailLog = await _context.EmailLogs.FindAsync(id);
            if (emailLog != null)
            {
                _context.EmailLogs.Remove(emailLog);
                await _context.SaveChangesAsync();
            }
        }
    }
}
