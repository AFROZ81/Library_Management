using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Repositories
{
    public interface ILibrarySettingsRepository
    {
        Task<LibrarySettings> GetSettingsAsync();
        Task UpdateSettingsAsync(LibrarySettings settings);
    }
}
