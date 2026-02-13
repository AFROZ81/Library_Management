using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
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
        public async Task ClearMemberFinesAsync(int memberId)
        {
            // Fetch active loans for this member that are overdue
            var overdueLoans = await _context.BookLoans
                .Where(l => l.MemberId == memberId && !l.IsReturned && l.DueDate < DateTime.Now.Date)
                .ToListAsync();

            foreach (var loan in overdueLoans)
            {
                // Calculate what the fine is right now
                int daysOverdue = (DateTime.Now.Date - loan.DueDate.Date).Days;
                decimal currentFine = daysOverdue * 10; // Assuming 10 per day

                // Update the database field we created in the migration
                loan.AmountPaid = currentFine;
            }

            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<BookLoan>> GetLoansByMemberIdAsync(int memberId)
        {
            return await _context.BookLoans
                .Include(l => l.Book) // Essential for showing titles in the profile history
                .Where(l => l.MemberId == memberId)
                .ToListAsync();
        }
    }
}