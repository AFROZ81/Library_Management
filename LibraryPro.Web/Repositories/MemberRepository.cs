using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _context;
        public MemberRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Member>> GetAllAsync() =>
            await _context.Members
                .Include(m => m.Loans)
                .ThenInclude(l => l.Book)
                .ToListAsync();

        public async Task<Member?> GetByIdAsync(int id) =>
            await _context.Members.FindAsync(id);

        public async Task<Member?> GetByEmailAsync(string email) =>
            await _context.Members.FirstOrDefaultAsync(m => m.Email == email);

        public async Task AddAsync(Member member)
        {
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Member member)
        {
            _context.Members.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var member = await _context.Members.FindAsync(id);
            if (member != null)
            {
                _context.Members.Remove(member);
                await _context.SaveChangesAsync();
            }
        }
    }
}