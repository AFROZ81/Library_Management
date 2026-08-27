using LibraryPro.Web.Models.Api;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class BooksApiController : ControllerBase
{
    private readonly IBookRepository _bookRepository;
    private readonly ILogger<BooksApiController> _logger;

    public BooksApiController(IBookRepository bookRepository, ILogger<BooksApiController> logger)
    {
        _bookRepository = bookRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all books
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiBookDto>>> GetBooks()
    {
        try
        {
            var books = await _bookRepository.GetAllAsync();
            var bookDtos = books.Select(b => new ApiBookDto
            {
                Id = b.Id,
                Title = b.Title ?? string.Empty,
                Author = b.Author ?? string.Empty,
                ISBN = b.ISBN ?? string.Empty,
                Genre = string.Join(", ", b.Genre),
                PublicationYear = b.PublicationYear,
                TotalCopies = b.TotalCopies,
                AvailableCopies = b.AvailableCopies,
                ImageUrl = b.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            return Ok(bookDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting books");
            return StatusCode(500, new { error = "An error occurred while retrieving books" });
        }
    }

    /// <summary>
    /// Get a book by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiBookDto>> GetBook(int id)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return NotFound(new { error = "Book not found" });
            }

            var bookDto = new ApiBookDto
            {
                Id = book.Id,
                Title = book.Title ?? string.Empty,
                Author = book.Author ?? string.Empty,
                ISBN = book.ISBN ?? string.Empty,
                Genre = string.Join(", ", book.Genre),
                PublicationYear = book.PublicationYear,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                ImageUrl = book.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            return Ok(bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting book with ID {BookId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the book" });
        }
    }

    /// <summary>
    /// Create a new book
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiBookDto>> CreateBook([FromBody] CreateBookDto createBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var book = new Book
            {
                Title = createBookDto.Title,
                Author = createBookDto.Author,
                ISBN = createBookDto.ISBN,
                Genre = createBookDto.Genre.Split(',').Select(g => g.Trim()).ToList(),
                PublicationYear = createBookDto.PublicationYear,
                TotalCopies = createBookDto.TotalCopies,
                AvailableCopies = createBookDto.TotalCopies,
                ImageUrl = createBookDto.ImageUrl
            };

            await _bookRepository.AddAsync(book);

            var bookDto = new ApiBookDto
            {
                Id = book.Id,
                Title = book.Title ?? string.Empty,
                Author = book.Author ?? string.Empty,
                ISBN = book.ISBN ?? string.Empty,
                Genre = string.Join(", ", book.Genre),
                PublicationYear = book.PublicationYear,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                ImageUrl = book.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating book");
            return StatusCode(500, new { error = "An error occurred while creating the book" });
        }
    }

    /// <summary>
    /// Update an existing book
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiBookDto>> UpdateBook(int id, [FromBody] UpdateBookDto updateBookDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return NotFound(new { error = "Book not found" });
            }

            book.Title = updateBookDto.Title;
            book.Author = updateBookDto.Author;
            book.ISBN = updateBookDto.ISBN;
            book.Genre = updateBookDto.Genre.Split(',').Select(g => g.Trim()).ToList();
            book.PublicationYear = updateBookDto.PublicationYear;
            book.TotalCopies = updateBookDto.TotalCopies;
            book.ImageUrl = updateBookDto.ImageUrl;

            await _bookRepository.UpdateAsync(book);

            var bookDto = new ApiBookDto
            {
                Id = book.Id,
                Title = book.Title ?? string.Empty,
                Author = book.Author ?? string.Empty,
                ISBN = book.ISBN ?? string.Empty,
                Genre = string.Join(", ", book.Genre),
                PublicationYear = book.PublicationYear,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                ImageUrl = book.ImageUrl,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return Ok(bookDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating book with ID {BookId}", id);
            return StatusCode(500, new { error = "An error occurred while updating the book" });
        }
    }

    /// <summary>
    /// Delete a book
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteBook(int id)
    {
        try
        {
            var book = await _bookRepository.GetByIdAsync(id);
            if (book == null)
            {
                return NotFound(new { error = "Book not found" });
            }

            await _bookRepository.DeleteAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting book with ID {BookId}", id);
            return StatusCode(500, new { error = "An error occurred while deleting the book" });
        }
    }
}
