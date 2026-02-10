using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface ILoanRepository
    {
        Task CreateLoanAsync(BookLoan loan);
        Task<IEnumerable<BookLoan>> GetAllLoansAsync();
        Task<BookLoan?> GetLoanByIdAsync(int id);
        Task UpdateLoanAsync(BookLoan loan);
    }
}