using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    [Authorize]
    public class ReservationsController : Controller
    {
        private readonly IBookReservationRepository _reservationRepo;
        private readonly IBookRepository _bookRepo;
        private readonly IMemberRepository _memberRepo;
        private readonly UserManager<IdentityUser> _userManager;

        public ReservationsController(
            IBookReservationRepository reservationRepo,
            IBookRepository bookRepo,
            IMemberRepository memberRepo,
            UserManager<IdentityUser> userManager)
        {
            _reservationRepo = reservationRepo;
            _bookRepo = bookRepo;
            _memberRepo = memberRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var member = (await _memberRepo.GetAllAsync()).FirstOrDefault(m => m.Email == user?.Email);

            if (User.IsInRole("Admin") || User.IsInRole("Librarian"))
            {
                var allReservations = await _reservationRepo.GetAllReservationsAsync();
                return View(allReservations);
            }
            
            if (member != null)
            {
                var memberReservations = await _reservationRepo.GetReservationsByMemberIdAsync(member.Id);
                return View(memberReservations);
            }

            return View(new List<BookReservation>());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int bookId)
        {
            var user = await _userManager.GetUserAsync(User);
            var member = (await _memberRepo.GetAllAsync()).FirstOrDefault(m => m.Email == user?.Email);

            if (member == null)
            {
                TempData["Error"] = "Member profile not found.";
                return RedirectToAction("Details", "Books", new { id = bookId });
            }

            var position = await _reservationRepo.GetQueuePositionAsync(bookId);
            
            var reservation = new BookReservation
            {
                BookId = bookId,
                MemberId = member.Id,
                ReservationDate = DateTime.Now,
                Status = ReservationStatus.Pending,
                QueuePosition = position,
                ExpirationDate = DateTime.Now.AddDays(7)
            };

            await _reservationRepo.AddReservationAsync(reservation);
            TempData["Success"] = $"Book reserved successfully. You are #{position} in the queue.";
            return RedirectToAction("Details", "Books", new { id = bookId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _reservationRepo.GetReservationByIdAsync(id);
            if (reservation != null)
            {
                reservation.Status = ReservationStatus.Cancelled;
                await _reservationRepo.UpdateReservationAsync(reservation);
                TempData["Success"] = "Reservation cancelled successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
