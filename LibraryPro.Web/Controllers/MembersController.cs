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
                .Where(l => !l.IsReturned && l.DueDate.Date < DateTime.Now.Date)
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
            // 1. Get the member entity
            var member = await _memberRepo.GetByIdAsync(id);
            if (member == null) return NotFound();

            // 2. Get the loans for this member (ensure you include Book data)
            var allLoans = await _loanRepo.GetLoansByMemberIdAsync(id);

            // 3. MAP the entity data into the ViewModel (The Missing Step!)
            var viewModel = new MemberProfileViewModel
            {
                MemberId = member.Id,
                Name = member.Name,
                Email = member.Email,
                // Calculate fines and separate active vs history
                TotalUnpaidFine = allLoans.Where(l => !l.IsReturned).Sum(l => l.CalculateLateFee),
                ActiveLoans = allLoans.Where(l => !l.IsReturned).ToList(),
                PastHistory = allLoans.Where(l => l.IsReturned).ToList()
            };

            // 4. Pass the VIEWMODEL to the view (not 'member')
            return View(viewModel);
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayFine(int memberId) // Make sure this name matches the 'name' attribute in your <input type="hidden">
        {
            if (memberId == 0) return BadRequest();

            await _loanRepo.ClearMemberFinesAsync(memberId);

            TempData["SuccessMessage"] = "Fines cleared successfully!";
            return RedirectToAction(nameof(Details), new { id = memberId });
        }
    }
}