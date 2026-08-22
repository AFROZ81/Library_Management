using LibraryPro.Web.Data;
using LibraryPro.Web.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Repositories
{
    public class LibrarySettingsRepository : ILibrarySettingsRepository
    {
        private readonly ApplicationDbContext _context;

        public LibrarySettingsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<LibrarySettings> GetSettingsAsync()
        {
            var settings = await _context.LibrarySettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new LibrarySettings();
                _context.LibrarySettings.Add(settings);
                await _context.SaveChangesAsync();
            }
            return settings;
        }

        public async Task UpdateSettingsAsync(LibrarySettings settings)
        {
            settings.UpdatedAt = DateTime.Now;
            _context.LibrarySettings.Update(settings);
            await _context.SaveChangesAsync();
        }
    }
}
