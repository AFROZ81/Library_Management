using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface IMemberRepository
    {
        Task<IEnumerable<Member>> GetAllAsync();
        Task AddAsync(Member member);
        Task<Member?> GetByIdAsync(int id);
        Task UpdateAsync(Member member); // Added for Edit functionality
        Task DeleteAsync(int id);       // Already in your interface
    }
}