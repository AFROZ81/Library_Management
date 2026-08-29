using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryPro.Tests.Repositories
{
    public class BookRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly BookRepository _repository;

        public BookRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new BookRepository(_context);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllBooks()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { Title = "Book 1", Author = "Author 1", ISBN = "1234567890123", Genre = "Fiction", TotalCopies = 5, AvailableCopies = 5 },
                new Book { Title = "Book 2", Author = "Author 2", ISBN = "1234567890124", Genre = "Non-Fiction", TotalCopies = 3, AvailableCopies = 3 }
            };
            await _context.Books.AddRangeAsync(books);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingBook_ReturnsBook()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Test Author", ISBN = "1234567890125", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(book.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Book", result.Title);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingBook_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_AddsBookToDatabase()
        {
            // Arrange
            var book = new Book { Title = "New Book", Author = "New Author", ISBN = "1234567890126", Genre = "Fiction", TotalCopies = 1, AvailableCopies = 1 };

            // Act
            await _repository.AddAsync(book);

            // Assert
            var addedBook = await _context.Books.FirstOrDefaultAsync(b => b.Title == "New Book");
            Assert.NotNull(addedBook);
            Assert.Equal("New Book", addedBook.Title);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesBookInDatabase()
        {
            // Arrange
            var book = new Book { Title = "Original Title", Author = "Author", ISBN = "1234567890127", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();

            book.Title = "Updated Title";

            // Act
            await _repository.UpdateAsync(book);

            // Assert
            var updatedBook = await _context.Books.FindAsync(book.Id);
            Assert.Equal("Updated Title", updatedBook.Title);
        }

        [Fact]
        public async Task DeleteAsync_ExistingBook_RemovesBookFromDatabase()
        {
            // Arrange
            var book = new Book { Title = "To Delete", Author = "Author", ISBN = "1234567890128", Genre = "Fiction", TotalCopies = 1, AvailableCopies = 1 };
            await _context.Books.AddAsync(book);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(book.Id);

            // Assert
            var deletedBook = await _context.Books.FindAsync(book.Id);
            Assert.Null(deletedBook);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingBook_DoesNotThrow()
        {
            // Act & Assert
            await _repository.DeleteAsync(999); // Should not throw
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
