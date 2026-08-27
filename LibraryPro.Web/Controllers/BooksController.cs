using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Controllers
{
    [Authorize]
    public sealed class BooksController : Controller
    {
        private readonly IBookRepository _bookRepo;
        private readonly IImageService _imageService;
        private readonly IRecommendationService _recommendationService;
        private readonly IBarcodeService _barcodeService;
        private readonly IExternalBookService _externalBookService;

        public BooksController(IBookRepository bookRepo, IImageService imageService, IRecommendationService recommendationService, IBarcodeService barcodeService, IExternalBookService externalBookService)
        {
            _bookRepo = bookRepo;
            _imageService = imageService;
            _recommendationService = recommendationService;
            _barcodeService = barcodeService;
            _externalBookService = externalBookService;
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
                books = books.Where(b => (b.Title != null && b.Title.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                      || (b.Author != null && b.Author.Contains(searchString, StringComparison.OrdinalIgnoreCase))
                                      || (b.Genre != null && b.Genre.Any(g => g.Contains(searchString, StringComparison.OrdinalIgnoreCase))));
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

        // GET: Advanced Search
        public IActionResult AdvancedSearch()
        {
            var model = new AdvancedSearchViewModel
            {
                SearchCriteria = new SearchCriteria(),
                PageNumber = 1,
                PageSize = 12
            };
            return View(model);
        }

        // POST: Advanced Search
        [HttpPost]
        public async Task<IActionResult> AdvancedSearch(AdvancedSearchViewModel model)
        {
            if (ModelState.IsValid)
            {
                var books = await _recommendationService.AdvancedSearchAsync(
                    model.SearchCriteria, 
                    model.PageNumber, 
                    model.PageSize);

                model.TotalResults = books.Count;
                return View("AdvancedSearchResults", model);
            }

            return View(model);
        }

        // GET: Recommendations
        public async Task<IActionResult> Recommendations()
        {
            var userId = User.Identity?.Name;
            var recommendations = await _recommendationService.GetRecommendationsAsync(userId, 10);
            return View(recommendations);
        }

        // GET: Recommendations by Book
        public async Task<IActionResult> RecommendationsByBook(int bookId)
        {
            var recommendations = await _recommendationService.GetRecommendationsByBookAsync(bookId, 5);
            return PartialView("_BookRecommendations", recommendations);
        }

        // GET: Lookup Book by ISBN
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> LookupByISBN(string isbn)
        {
            if (string.IsNullOrEmpty(isbn))
            {
                return Json(new { success = false, message = "ISBN is required" });
            }

            var metadata = await _externalBookService.SearchByISBNAsync(isbn);
            if (metadata == null)
            {
                return Json(new { success = false, message = "Book not found" });
            }

            return Json(new { success = true, data = metadata });
        }

        // GET: Create Book Form
        [Authorize(Policy = "LibrarianOrAdmin")]
        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> Create(Book book, IFormFile? imageFile)
        {
            if (ModelState.IsValid)
            {
                // Generate barcode for the book
                book.Barcode = _barcodeService.GenerateBarcodeText(book.ISBN ?? book.Title ?? string.Empty);

                // Handle image upload
                if (imageFile != null)
                {
                    if (_imageService.ValidateImage(imageFile, out var errorMessage))
                    {
                        book.ImageUrl = await _imageService.SaveImageAsync(imageFile, "images/books");
                    }
                    else
                    {
                        ModelState.AddModelError("ImageFile", errorMessage);
                        return View(book);
                    }
                }
                else
                {
                    book.ImageUrl = _imageService.GetDefaultImagePath();
                }

                // Logic: Initial stock is always fully available
                book.AvailableCopies = book.TotalCopies;

                await _bookRepo.AddAsync(book);
                TempData["Success"] = "New volume has been added.";
                return RedirectToAction(nameof(Index));
            }
            return View(book);
        }

        // GET: Books/Edit/5
        [Authorize(Policy = "LibrarianOrAdmin")]
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
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> Edit(int id, Book book, IFormFile? imageFile)
{
    if (id != book.Id) return NotFound();

    if (ModelState.IsValid)
    {
        try
        {
            // 1. Fetch the tracked entity
            var existingBook = await _bookRepo.GetByIdAsync(id);
            if (existingBook == null) return NotFound();

            // 2. Handle image upload
            if (imageFile != null)
            {
                if (_imageService.ValidateImage(imageFile, out var errorMessage))
                {
                    // Delete old image if it exists and is not the default
                    if (existingBook.ImageUrl != null && 
                        !existingBook.ImageUrl.Contains("default-book-cover"))
                    {
                        _imageService.DeleteImage(existingBook.ImageUrl);
                    }
                    
                    existingBook.ImageUrl = await _imageService.SaveImageAsync(imageFile, "images/books");
                }
                else
                {
                    ModelState.AddModelError("ImageFile", errorMessage);
                    return View(book);
                }
            }

            // 3. Calculate copy differences
            int difference = book.TotalCopies - existingBook.TotalCopies;
            
            // 4. Update the tracked entity's properties manually
            existingBook.Title = book.Title;
            existingBook.Author = book.Author;
            existingBook.ISBN = book.ISBN;
            existingBook.Genre = book.Genre;
            existingBook.PublicationYear = book.PublicationYear;
            existingBook.TotalCopies = book.TotalCopies;
            
            // Update available copies based on the change in stock
            existingBook.AvailableCopies += difference;
            if (existingBook.AvailableCopies < 0) existingBook.AvailableCopies = 0;

            // 5. Save the tracked entity
            await _bookRepo.UpdateAsync(existingBook);
        }
        catch (Exception)
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
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookRepo.GetByIdAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Policy = "LibrarianOrAdmin")]
        public async Task<IActionResult> DeleteConfirmed([FromRoute] int id) // Add [FromRoute]
        {
            await _bookRepo.DeleteAsync(id);
            TempData["Success"] = "The volume has been permanently removed.";
            return RedirectToAction(nameof(Index));
        }
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            // Use the repository instead of the context
            var books = await _bookRepo.GetByIdAsync(id.Value);

            if (books == null) return NotFound();

            return View(books);
        }
    }
}