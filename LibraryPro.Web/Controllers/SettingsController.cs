using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ILibrarySettingsRepository _settingsRepo;

        public SettingsController(ILibrarySettingsRepository settingsRepo)
        {
            _settingsRepo = settingsRepo;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _settingsRepo.GetSettingsAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LibrarySettings settings)
        {
            if (ModelState.IsValid)
            {
                await _settingsRepo.UpdateSettingsAsync(settings);
                TempData["Success"] = "Library policy settings updated successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(settings);
        }
    }
}
