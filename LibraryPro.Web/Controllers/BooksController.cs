using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Controllers
{
    public sealed class BooksController : Controller
    {
        private readonly IBookRepository _bookRepo;

        public BooksController(IBookRepository bookRepo)
        {
            _bookRepo = bookRepo;
        }

        // GET: All Books
        public async Task<IActionResult> Index(string searchString)
        {
            var books = await _bookRepo.GetAllAsync();

            if (!string.IsNullOrEmpty(searchString))
            {
                // Updated to search within the Genre list as well
                books = books.Where(b => b.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                      || b.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                      || b.Genre.Any(g => g.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
            }

            ViewData["CurrentFilter"] = searchString;
            return View(books);
        }

        // GET: Create Book Form
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Book book)
        {
            if (ModelState.IsValid)
            {
                // Logic: Initial stock is always fully available
                book.AvailableCopies = book.TotalCopies;

                await _bookRepo.AddAsync(book);
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookRepo.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Book book)
        {
            if (id != book.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // CRITICAL: We need to preserve AvailableCopies logic
                    // If you increase TotalCopies, we should increase AvailableCopies by the same amount
                    var existingBook = await _bookRepo.GetByIdAsync(id);
                    if (existingBook != null)
                    {
                        int difference = book.TotalCopies - existingBook.TotalCopies;
                        book.AvailableCopies = existingBook.AvailableCopies + difference;

                        // Prevent available copies from going negative
                        if (book.AvailableCopies < 0) book.AvailableCopies = 0;
                    }

                    await _bookRepo.UpdateAsync(book);
                }
                catch (Exception)
                {
                    ModelState.AddModelError("", "Unable to save changes. Try again.");
                    return View(book);
                }
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookRepo.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Books/Delete/5
        // This handles the actual deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var book = await _bookRepo.GetByIdAsync(id);
            if (book != null)
            {
                await _bookRepo.DeleteAsync(id); // Using the repo instead of _context
            }

            // Redirect back to the index so the user sees the updated list
            return RedirectToAction(nameof(Index));
        }
    }
}