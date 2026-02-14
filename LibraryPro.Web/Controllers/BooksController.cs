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
        public async Task<IActionResult> Index(string sortOrder, string currentFilter, string searchString, int? pageNumber)
        {
            ViewData["CurrentSort"] = sortOrder;
            // Toggle logic for sorting
            ViewData["TitleSortParm"] = String.IsNullOrEmpty(sortOrder) ? "title_desc" : "";
            ViewData["AuthorSortParm"] = sortOrder == "Author" ? "author_desc" : "Author";
            ViewData["YearSortParm"] = sortOrder == "Year" ? "year_desc" : "Year";

            if (searchString != null) pageNumber = 1;
            else searchString = currentFilter;

            ViewData["CurrentFilter"] = searchString;

            var books = await _bookRepo.GetAllAsync();

            // 1. Search Filter
            if (!string.IsNullOrEmpty(searchString))
            {
                books = books.Where(b => b.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                      || b.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase)
                                      || b.Genre.Any(g => g.Contains(searchString, StringComparison.OrdinalIgnoreCase)));
            }

            // 2. Advanced Sorting
            books = sortOrder switch
            {
                "title_desc" => books.OrderByDescending(b => b.Title),
                "Author" => books.OrderBy(b => b.Author),
                "author_desc" => books.OrderByDescending(b => b.Author),
                "Year" => books.OrderBy(b => b.PublicationYear),
                "year_desc" => books.OrderByDescending(b => b.PublicationYear),
                _ => books.OrderBy(b => b.Title),
            };

            // 3. Pagination
            int pageSize = 8;
            return View(await PaginatedList<Book>.CreateAsync(books, pageNumber ?? 1, pageSize));
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
            // 1. Fetch the tracked entity
            var existingBook = await _bookRepo.GetByIdAsync(id);
            if (existingBook == null) return NotFound();

            // 2. Calculate copy differences
            int difference = book.TotalCopies - existingBook.TotalCopies;
            
            // 3. Update the tracked entity's properties manually
            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.ISBN = book.ISBN;
            existingBook.Genre = book.Genre;
            existingBook.PublicationYear = book.PublicationYear;
            existingBook.TotalCopies = book.TotalCopies;
            
            // Update available copies based on the change in stock
            existingBook.AvailableCopies += difference;
            if (existingBook.AvailableCopies < 0) existingBook.AvailableCopies = 0;

            // 4. Save the tracked entity
            await _bookRepo.UpdateAsync(existingBook);
        }
        catch (Exception ex)
        {
            // Helpful for debugging: check ex.Message in your debugger
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id) // Add [FromRoute]
        {
            await _bookRepo.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Use the repository instead of the context
            var book = await _bookRepo.GetByIdAsync(id.Value);

            if (book == null) return NotFound();

            return View(book);
        }
    }
}