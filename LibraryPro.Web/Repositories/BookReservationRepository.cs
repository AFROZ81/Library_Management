using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class BookReservationRepository : IBookReservationRepository
    {
        private readonly ApplicationDbContext _context;

        public BookReservationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<BookReservation>> GetAllReservationsAsync()
        {
            return await _context.BookReservations
                .Include(r => r.Book)
                .Include(r => r.Member)
                .OrderBy(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<BookReservation>> GetReservationsByMemberIdAsync(int memberId)
        {
            return await _context.BookReservations
                .Include(r => r.Book)
                .Where(r => r.MemberId == memberId)
                .OrderByDescending(r => r.ReservationDate)
                .ToListAsync();
        }

        public async Task<BookReservation?> GetReservationByIdAsync(int id)
        {
            return await _context.BookReservations
                .Include(r => r.Book)
                .Include(r => r.Member)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task AddReservationAsync(BookReservation reservation)
        {
            await _context.BookReservations.AddAsync(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateReservationAsync(BookReservation reservation)
        {
            _context.BookReservations.Update(reservation);
            await _context.SaveChangesAsync();
        }

        public async Task<int> GetQueuePositionAsync(int bookId)
        {
            var reservations = await _context.BookReservations
                .Where(r => r.BookId == bookId && r.Status == ReservationStatus.Pending)
                .CountAsync();
            return reservations + 1;
        }

        public async Task DeleteReservationAsync(int id)
        {
            var reservation = await _context.BookReservations.FindAsync(id);
            if (reservation != null)
            {
                _context.BookReservations.Remove(reservation);
                await _context.SaveChangesAsync();
            }
        }
    }
}
