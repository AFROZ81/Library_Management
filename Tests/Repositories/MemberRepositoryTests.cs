using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryPro.Tests.Repositories
{
    public class MemberRepositoryTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly MemberRepository _repository;

        public MemberRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _repository = new MemberRepository(_context);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllMembers()
        {
            // Arrange
            var members = new List<Member>
            {
                new Member { Name = "John Doe", Email = "john@example.com", PhoneNumber = "1234567890", MembershipDate = DateTime.UtcNow },
                new Member { Name = "Jane Smith", Email = "jane@example.com", PhoneNumber = "0987654321", MembershipDate = DateTime.UtcNow }
            };
            await _context.Members.AddRangeAsync(members);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetByIdAsync_ExistingMember_ReturnsMember()
        {
            // Arrange
            var member = new Member { Name = "Test Member", Email = "test@example.com", PhoneNumber = "1111111111", MembershipDate = DateTime.UtcNow };
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetByIdAsync(member.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Test Member", result.Name);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingMember_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_AddsMemberToDatabase()
        {
            // Arrange
            var member = new Member { Name = "New Member", Email = "new@example.com", PhoneNumber = "2222222222", MembershipDate = DateTime.UtcNow };

            // Act
            await _repository.AddAsync(member);

            // Assert
            var addedMember = await _context.Members.FirstOrDefaultAsync(m => m.Name == "New Member");
            Assert.NotNull(addedMember);
            Assert.Equal("New Member", addedMember.Name);
        }

        [Fact]
        public async Task UpdateAsync_UpdatesMemberInDatabase()
        {
            // Arrange
            var member = new Member { Name = "Original Name", Email = "original@example.com", PhoneNumber = "3333333333", MembershipDate = DateTime.UtcNow };
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            member.Name = "Updated Name";

            // Act
            await _repository.UpdateAsync(member);

            // Assert
            var updatedMember = await _context.Members.FindAsync(member.Id);
            Assert.Equal("Updated Name", updatedMember.Name);
        }

        [Fact]
        public async Task DeleteAsync_ExistingMember_RemovesMemberFromDatabase()
        {
            // Arrange
            var member = new Member { Name = "To Delete", Email = "delete@example.com", PhoneNumber = "4444444444", MembershipDate = DateTime.UtcNow };
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();

            // Act
            await _repository.DeleteAsync(member.Id);

            // Assert
            var deletedMember = await _context.Members.FindAsync(member.Id);
            Assert.Null(deletedMember);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
