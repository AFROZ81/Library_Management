using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace LibraryPro.Web.Controllers
{
    public class LoansController : Controller
    {
        private readonly ILoanRepository _loanRepo;
        private readonly IBookRepository _bookRepo;
        private readonly IMemberRepository _memberRepo;

        public LoansController(ILoanRepository loanRepo, IBookRepository bookRepo, IMemberRepository memberRepo)
        {
            _loanRepo = loanRepo;
            _bookRepo = bookRepo;
            _memberRepo = memberRepo;
        }

        public async Task<IActionResult> Index()
        {
            var loans = await _loanRepo.GetAllLoansAsync();
            return View(loans);
        }

        public async Task<IActionResult> Issue()
        {
            // Get data for dropdowns
            var books = await _bookRepo.GetAllAsync();
            var members = await _memberRepo.GetAllAsync();

            ViewBag.Books = new SelectList(books.Where(b => b.AvailableCopies > 0), "Id", "Title");
            ViewBag.Members = new SelectList(members, "Id", "Name");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Issue(BookLoan loan)
        {
            // Professional Tip: Remove validation for navigation properties 
            // so the binder only cares about the IDs you sent.
            ModelState.Remove("Book");
            ModelState.Remove("Member");

            if (loan.BookId <= 0 || loan.MemberId <= 0)
            {
                ModelState.AddModelError("", "Please select both a book and a member.");
            }

            if (ModelState.IsValid)
            {
                var book = await _bookRepo.GetByIdAsync(loan.BookId);
                if (book != null && book.AvailableCopies > 0)
                {
                    book.AvailableCopies--;
                    await _bookRepo.UpdateAsync(book);
                    await _loanRepo.CreateLoanAsync(loan);
                    return RedirectToAction(nameof(Index));
                }
                else
                {
                    ModelState.AddModelError("", "This book is no longer available.");
                }
            }

            // RELOAD DROPDOWNS: If validation fails, we MUST reload these or the page crashes/empties
            var books = await _bookRepo.GetAllAsync();
            var members = await _memberRepo.GetAllAsync();
            ViewBag.Books = new SelectList(books.Where(b => b.AvailableCopies > 0), "Id", "Title");
            ViewBag.Members = new SelectList(members, "Id", "Name");

            return View(loan);
        }

        [HttpPost]
        public async Task<IActionResult> Return(int loanId)
        {
            var loan = await _loanRepo.GetLoanByIdAsync(loanId);
            if (loan != null && !loan.IsReturned)
            {
                // 1. Mark as returned
                loan.IsReturned = true;
                loan.ReturnDate = DateTime.Now;

                // 2. Increase Book Stock
                var book = await _bookRepo.GetByIdAsync(loan.BookId);
                if (book != null)
                {
                    book.AvailableCopies++;
                    await _bookRepo.UpdateAsync(book);
                }

                await _loanRepo.UpdateLoanAsync(loan);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}