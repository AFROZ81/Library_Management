using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly ApplicationDbContext _context;
        public LoanRepository(ApplicationDbContext context) => _context = context;

        public async Task CreateLoanAsync(BookLoan loan)
        {
            await _context.BookLoans.AddAsync(loan);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<BookLoan>> GetAllLoansAsync() =>
            await _context.BookLoans.Include(l => l.Book).Include(l => l.Member).ToListAsync();

        public async Task<BookLoan?> GetLoanByIdAsync(int id) =>
            await _context.BookLoans.FindAsync(id);

        public async Task UpdateLoanAsync(BookLoan loan)
        {
            _context.BookLoans.Update(loan);
            await _context.SaveChangesAsync();
        }
    }
}