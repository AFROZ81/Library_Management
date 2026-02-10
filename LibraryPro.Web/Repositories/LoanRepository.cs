using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly ApplicationDbContext _context;

        public LoanRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<BookLoan>> GetAllLoansAsync()
        {
            return await _context.BookLoans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .ToListAsync();
        }

        public async Task<BookLoan?> GetLoanByIdAsync(int id)
        {
            return await _context.BookLoans
                .Include(l => l.Book)
                .Include(l => l.Member)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task CreateLoanAsync(BookLoan loan)
        {
            await _context.BookLoans.AddAsync(loan);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLoanAsync(BookLoan loan)
        {
            _context.BookLoans.Update(loan);
            await _context.SaveChangesAsync();
        }
    }
}