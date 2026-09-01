using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Models.ViewModels;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    [Authorize]
    public class MemberDashboardController : Controller
    {
        private readonly IMemberRepository _memberRepo;
        private readonly ILoanRepository _loanRepo;
        private readonly IBookReservationRepository _reservationRepo;
        private readonly UserManager<IdentityUser> _userManager;

        public MemberDashboardController(
            IMemberRepository memberRepo,
            ILoanRepository loanRepo,
            IBookReservationRepository reservationRepo,
            UserManager<IdentityUser> userManager)
        {
            _memberRepo = memberRepo;
            _loanRepo = loanRepo;
            _reservationRepo = reservationRepo;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _memberRepo.GetByEmailAsync(user?.Email);

            if (member == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var allLoans = await _loanRepo.GetAllLoansAsync();
            var memberLoans = allLoans.Where(l => l.MemberId == member.Id).ToList();
            var memberReservations = await _reservationRepo.GetReservationsByMemberIdAsync(member.Id);

            var currentLoans = memberLoans.Where(l => !l.IsReturned).ToList();
            var overdueLoans = currentLoans.Where(l => l.DueDate.Date < DateTime.Now.Date).ToList();
            var totalFines = overdueLoans.Sum(l => (decimal)(DateTime.Now.Date - l.DueDate.Date).TotalDays * 10);

            var viewModel = new MemberDashboardViewModel
            {
                Member = member,
                CurrentLoans = currentLoans,
                OverdueLoans = overdueLoans,
                TotalFines = totalFines,
                ActiveReservations = memberReservations.Where(r => r.Status == ReservationStatus.Pending).ToList(),
                TotalBorrowedBooks = memberLoans.Count,
                LoanHistory = memberLoans.Where(l => l.IsReturned).OrderByDescending(l => l.ReturnDate).Take(10).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _memberRepo.GetByEmailAsync(user?.Email);

            if (member == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(Member member)
        {
            var user = await _userManager.GetUserAsync(User);
            var existingMember = await _memberRepo.GetByEmailAsync(user?.Email);

            if (existingMember == null || existingMember.Id != member.Id)
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                existingMember.Name = member.Name;
                existingMember.PhoneNumber = member.PhoneNumber;
                existingMember.ReceiveDueDateReminders = member.ReceiveDueDateReminders;
                existingMember.ReceiveOverdueNotices = member.ReceiveOverdueNotices;
                existingMember.ReceiveReservationAlerts = member.ReceiveReservationAlerts;

                await _memberRepo.UpdateAsync(existingMember);
                TempData["Success"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Profile));
            }

            return View(member);
        }

        public async Task<IActionResult> BorrowingHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var member = await _memberRepo.GetByEmailAsync(user?.Email);

            if (member == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var allLoans = await _loanRepo.GetAllLoansAsync();
            var memberLoans = allLoans.Where(l => l.MemberId == member.Id).OrderByDescending(l => l.LoanDate).ToList();

            return View(memberLoans);
        }
    }
}