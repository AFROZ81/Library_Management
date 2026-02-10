using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    public class MembersController : Controller
    {
        private readonly IMemberRepository _memberRepo;
        // 1. Add the private field for Loan Repository
        private readonly ILoanRepository _loanRepo;

        // 2. Update the constructor to inject BOTH repositories
        public MembersController(IMemberRepository memberRepo, ILoanRepository loanRepo)
        {
            _memberRepo = memberRepo;
            _loanRepo = loanRepo;
        }

        public async Task<IActionResult> Index()
        {
            var members = await _memberRepo.GetAllAsync();
            var allLoans = await _loanRepo.GetAllLoansAsync();

            // Create a lookup for members who have overdue books
            var overdueMemberIds = allLoans
                .Where(l => !l.IsReturned && l.DueDate < DateTime.Now)
                .Select(l => l.MemberId)
                .Distinct()
                .ToList();

            ViewBag.OverdueMemberIds = overdueMemberIds;
            return View(members);
        }
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(Member member)
        {
            if (ModelState.IsValid)
            {
                await _memberRepo.AddAsync(member);
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        public async Task<IActionResult> Details(int id)
        {
            var member = await _memberRepo.GetByIdAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var member = await _memberRepo.GetByIdAsync(id);
            if (member == null) return NotFound();
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Member member)
        {
            if (id != member.Id) return NotFound();

            if (ModelState.IsValid)
            {
                await _memberRepo.UpdateAsync(member);
                return RedirectToAction(nameof(Index));
            }
            return View(member);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var member = await _memberRepo.GetByIdAsync(id);
            if (member != null)
            {
                await _memberRepo.DeleteAsync(id);
            }
            return RedirectToAction(nameof(Index));
        }

        // 3. This will now work because _loanRepo is defined!
        public async Task<IActionResult> Profile(int id)
        {
            var member = await _memberRepo.GetByIdAsync(id);
            if (member == null) return NotFound();

            var allLoans = await _loanRepo.GetAllLoansAsync();
            var memberLoans = allLoans.Where(l => l.MemberId == id);

            var viewModel = new MemberProfileViewModel
            {
                MemberId = member.Id,
                Name = member.Name,
                Email = member.Email,
                TotalUnpaidFine = memberLoans.Where(l => !l.IsReturned).Sum(l => l.CalculateLateFee),
                ActiveLoans = memberLoans.Where(l => !l.IsReturned).ToList(),
                PastHistory = memberLoans.Where(l => l.IsReturned).OrderByDescending(l => l.ReturnDate).ToList()
            };

            return View(viewModel);
        }
        [HttpPost]
        public async Task<IActionResult> PayFine(int memberId)
        {
            var loans = await _loanRepo.GetAllLoansAsync();
            var memberLoans = loans.Where(l => l.MemberId == memberId && !l.IsReturned);

            foreach (var loan in memberLoans)
            {
                // In a real-world app, you'd mark the fine as 'Paid' 
                // For now, we can reset the metadata or log the transaction
            }

            return RedirectToAction(nameof(Profile), new { id = memberId });
        }
    }
}