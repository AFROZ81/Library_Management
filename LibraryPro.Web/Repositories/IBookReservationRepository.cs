using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface IBookReservationRepository
    {
        Task<IEnumerable<BookReservation>> GetAllReservationsAsync();
        Task<IEnumerable<BookReservation>> GetReservationsByMemberIdAsync(int memberId);
        Task<BookReservation?> GetReservationByIdAsync(int id);
        Task AddReservationAsync(BookReservation reservation);
        Task UpdateReservationAsync(BookReservation reservation);
        Task<int> GetQueuePositionAsync(int bookId);
        Task DeleteReservationAsync(int id);
    }
}
