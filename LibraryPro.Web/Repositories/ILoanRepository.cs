using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface ILoanRepository
    {
        Task<IEnumerable<BookLoan>> GetAllLoansAsync(); // Renamed
        Task<BookLoan?> GetLoanByIdAsync(int id);      // Renamed
        Task CreateLoanAsync(BookLoan loan);           // Renamed
        Task UpdateLoanAsync(BookLoan loan);           // Renamed
    }
}