using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryPro.Tests.Repositories
{
    public class LoanRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly LoanRepository _repository;

        public LoanRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new LoanRepository(_context);
        }

        [Fact]
        public async Task GetAllLoansAsync_ReturnsAllLoansWithIncludes()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890123", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var loan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            await _context.BookLoans.AddAsync(loan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllLoansAsync();

            // Assert
            Assert.Single(result);
            Assert.NotNull(result.First().Book);
            Assert.NotNull(result.First().Member);
        }

        [Fact]
        public async Task GetLoanByIdAsync_ExistingLoan_ReturnsLoanWithIncludes()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890124", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var loan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            await _context.BookLoans.AddAsync(loan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLoanByIdAsync(loan.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotNull(result.Book);
            Assert.NotNull(result.Member);
        }

        [Fact]
        public async Task CreateLoanAsync_AddsLoanToDatabase()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890125", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var loan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };

            // Act
            await _repository.CreateLoanAsync(loan);

            // Assert
            var addedLoan = await _context.BookLoans.FirstOrDefaultAsync(l => l.BookId == book.Id);
            Assert.NotNull(addedLoan);
        }

        [Fact]
        public async Task UpdateLoanAsync_UpdatesLoanInDatabase()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890126", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var loan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            await _context.BookLoans.AddAsync(loan);
            await _context.SaveChangesAsync();

            loan.IsReturned = true;
            loan.ReturnDate = DateTime.UtcNow;

            // Act
            await _repository.UpdateLoanAsync(loan);

            // Assert
            var updatedLoan = await _context.BookLoans.FindAsync(loan.Id);
            Assert.True(updatedLoan.IsReturned);
        }

        [Fact]
        public async Task GetOverdueLoansAsync_ReturnsOnlyOverdueLoans()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890127", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            var overdueLoan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow.AddDays(-20), DueDate = DateTime.UtcNow.AddDays(-5) };
            var activeLoan = new BookLoan { BookId = book.Id, MemberId = member.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            await _context.BookLoans.AddRangeAsync(overdueLoan, activeLoan);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOverdueLoansAsync();

            // Assert
            Assert.Single(result);
            Assert.Equal(overdueLoan.Id, result.First().Id);
        }

        [Fact]
        public async Task GetLoansByMemberIdAsync_ReturnsLoansForMember()
        {
            // Arrange
            var book = new Book { Title = "Test Book", Author = "Author", ISBN = "1234567890128", Genre = "Fiction", TotalCopies = 2, AvailableCopies = 2 };
            var member1 = new Member { Name = "Member 1", Email = "member1@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow };
            var member2 = new Member { Name = "Member 2", Email = "member2@example.com", PhoneNumber = "0987654321", MembershipDate = DateTime.UtcNow };
            await _context.Books.AddAsync(book);
            await _context.Members.AddRangeAsync(member1, member2);
            await _context.SaveChangesAsync();

            var loan1 = new BookLoan { BookId = book.Id, MemberId = member1.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            var loan2 = new BookLoan { BookId = book.Id, MemberId = member2.Id, LoanDate = DateTime.UtcNow, DueDate = DateTime.UtcNow.AddDays(14) };
            await _context.BookLoans.AddRangeAsync(loan1, loan2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLoansByMemberIdAsync(member1.Id);

            // Assert
            Assert.Single(result);
            Assert.Equal(member1.Id, result.First().MemberId);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
