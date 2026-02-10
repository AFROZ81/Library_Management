using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface IMemberRepository
    {
        Task<IEnumerable<Member>> GetAllAsync();
        Task AddAsync(Member member);
        Task<Member?> GetByIdAsync(int id);
    }
}