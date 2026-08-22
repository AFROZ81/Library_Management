using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
using LibraryPro.Web.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Ensure database is created and migrations applied
            await _context.Database.MigrateAsync();

            // Seed roles
            await SeedRolesAsync();

            // Seed default admin user
            await SeedAdminUserAsync();

            // Seed default library settings
            await SeedLibrarySettingsAsync();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { Constants.Roles.Admin, Constants.Roles.Librarian, Constants.Roles.Member };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            const string adminEmail = "admin@librarypro.com";
            const string adminPassword = "Admin@1234";

            var existingAdmin = await _userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, Constants.Roles.Admin);
                }
            }
        }

        private async Task SeedLibrarySettingsAsync()
        {
            if (!await _context.LibrarySettings.AnyAsync())
            {
                _context.LibrarySettings.Add(new LibrarySettings
                {
                    DailyFineRate = 10.00m,
                    DefaultLoanPeriodDays = 14,
                    MaxBooksPerMember = 5,
                    MaxRenewalAttempts = 2,
                    GracePeriodDays = 0,
                    UpdatedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}
