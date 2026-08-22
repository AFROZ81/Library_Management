using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryPro.Web.Controllers
{
    [Authorize(Policy = "LibrarianOrAdmin")]
    public class LoansController : Controller
    {
        private readonly ILoanRepository _loanRepo;
        private readonly IBookRepository _bookRepo;
        private readonly IMemberRepository _memberRepo;
        private readonly ILibrarySettingsRepository _settingsRepo;

        public LoansController(
            ILoanRepository loanRepo, 
            IBookRepository bookRepo, 
            IMemberRepository memberRepo,
            ILibrarySettingsRepository settingsRepo)
        {
            _loanRepo = loanRepo;
            _bookRepo = bookRepo;
            _memberRepo = memberRepo;
            _settingsRepo = settingsRepo;
        }

        public async Task<IActionResult> Index()
        {
            var members = await _memberRepo.GetAllAsync();
            var settings = await _settingsRepo.GetSettingsAsync();
            ViewBag.Settings = settings;
            return View(members);
        }

        public async Task<IActionResult> Issue()
        {
            var books = await _bookRepo.GetAllAsync();
            var members = await _memberRepo.GetAllAsync();
            var settings = await _settingsRepo.GetSettingsAsync();

            ViewBag.Books = new SelectList(books.Where(b => b.AvailableCopies > 0), "Id", "Title");
            ViewBag.Members = new SelectList(members, "Id", "Name");
            
            var loan = new BookLoan
            {
                LoanDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(settings.DefaultLoanPeriodDays)
            };

            return View(loan);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(BookLoan loan)
        {
            if (ModelState.IsValid)
            {
                var settings = await _settingsRepo.GetSettingsAsync();
                var memberLoans = await _loanRepo.GetLoansByMemberIdAsync(loan.MemberId);
                
                if (memberLoans.Count(l => !l.IsReturned) >= settings.MaxBooksPerMember)
                {
                    TempData["Error"] = $"Member has reached the maximum limit of {settings.MaxBooksPerMember} active loans.";
                    return RedirectToAction(nameof(Index));
                }

                var book = await _bookRepo.GetByIdAsync(loan.BookId);
                if (book != null && book.AvailableCopies > 0)
                {
                    book.AvailableCopies--;
                    await _bookRepo.UpdateAsync(book);

                    await _loanRepo.CreateLoanAsync(loan);
                    TempData["Success"] = "Book issued successfully.";
                    return RedirectToAction(nameof(Index));
                }
                TempData["Error"] = "Book is not available.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Return(int loanId)
        {
            var loan = await _loanRepo.GetLoanByIdAsync(loanId);
            if (loan != null && !loan.IsReturned)
            {
                loan.IsReturned = true;
                loan.ReturnDate = DateTime.Now;
                await _loanRepo.UpdateLoanAsync(loan);

                var book = await _bookRepo.GetByIdAsync(loan.BookId);
                if (book != null)
                {
                    book.AvailableCopies++;
                    await _bookRepo.UpdateAsync(book);
                }

                TempData["Success"] = "Book returned successfully.";
            }
            return RedirectToAction(nameof(Index));
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Renew(int loanId)
        {
            var loan = await _loanRepo.GetLoanByIdAsync(loanId);
            var settings = await _settingsRepo.GetSettingsAsync();

            if (loan != null && !loan.IsReturned)
            {
                if (loan.RenewalCount < settings.MaxRenewalAttempts)
                {
                    loan.RenewalCount++;
                    loan.LastRenewalDate = DateTime.Now;
                    loan.DueDate = loan.DueDate.AddDays(settings.DefaultLoanPeriodDays);
                    await _loanRepo.UpdateLoanAsync(loan);
                    TempData["Success"] = "Loan renewed successfully.";
                }
                else
                {
                    TempData["Error"] = "Maximum renewal attempts reached.";
                }
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
