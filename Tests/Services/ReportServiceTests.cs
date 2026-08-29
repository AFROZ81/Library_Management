using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraryPro.Tests.Services
{
    public class ReportServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly ReportService _reportService;
        private readonly Mock<ILogger<ReportService>> _loggerMock;

        public ReportServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            var loanRepository = new LoanRepository(_context);
            var bookRepository = new BookRepository(_context);
            var memberRepository = new MemberRepository(_context);
            _loggerMock = new Mock<ILogger<ReportService>>();

            _reportService = new ReportService(
                loanRepository,
                bookRepository,
                memberRepository,
                _context,
                _loggerMock.Object);
        }

        [Fact]
        public async Task GetCirculationReportDataAsync_ReturnsCorrectData()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890123", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var startDate = DateTime.UtcNow.AddDays(-10);
            var endDate = DateTime.UtcNow;

            var loan1 = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow.AddDays(-5), DueDate = DateTime.UtcNow.AddDays(9), IsReturned = true, ReturnDate = DateTime.UtcNow.AddDays(-2) };
            var loan2 = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow.AddDays(-3), DueDate = DateTime.UtcNow.AddDays(11), IsReturned = false };
            await _context.BookLoans.AddRangeAsync(loan1, loan2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reportService.GetCirculationReportDataAsync(startDate, endDate);

            // Assert
            Assert.Equal(2, result.TotalLoansIssued);
            Assert.Equal(1, result.TotalLoansReturned);
            Assert.Equal(1, result.ActiveLoans);
        }

        [Fact]
        public async Task GetInventoryReportDataAsync_ReturnsCorrectData()
        {
            // Arrange
            var books = new List<Book>
            {
                new Book { Title = "Book 1", Author = "Author 1", ISBN = "1234567890124", Genre = "Fiction", TotalCopies = 5, AvailableCopies = 3 },
                new Book { Title = "Book 2", Author = "Author 2", ISBN = "1234567890125", Genre = "Non-Fiction", TotalCopies = 3, AvailableCopies = 0 }
            };
            await _context.Books.AddRangeAsync(books);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reportService.GetInventoryReportDataAsync();

            // Assert
            Assert.Equal(2, result.TotalBooks);
            Assert.Equal(8, result.TotalCopies);
            Assert.Equal(3, result.AvailableCopies);
            Assert.Equal(5, result.CheckedOutCopies);
        }

        [Fact]
        public async Task GetOverdueBooksReportDataAsync_ReturnsOnlyOverdueBooks()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890126", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var overdueLoan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(-5), IsReturned = false };
            var activeLoan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14), IsReturned = false };
            await _context.BookLoans.AddRangeAsync(overdueLoan, activeLoan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _reportService.GetOverdueBooksReportDataAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(overdueLoan.Id, result.First().LoanId);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
