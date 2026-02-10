using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly ApplicationDbContext _context;
        public MemberRepository(ApplicationDbContext context) => _context = context;

        public async Task<IEnumerable<Member>> GetAllAsync() => await _context.Members.ToListAsync();

        public async Task AddAsync(Member member)
        {
            await _context.Members.AddAsync(member);
            await _context.SaveChangesAsync();
        }

        public async Task<Member?> GetByIdAsync(int id) => await _context.Members.FindAsync(id);
    }
}