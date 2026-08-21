using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryPro.Web.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            // Apply pending migrations
            try
            {
                await _context.Database.MigrateAsync();
                _logger.LogInformation("Database migrations applied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying database migrations.");
                throw;
            }

            // Seed roles
            await SeedRolesAsync();

            // Seed admin user
            await SeedAdminUserAsync();
        }

        private async Task SeedRolesAsync()
        {
            var roles = new[] { Constants.Roles.Admin, Constants.Roles.Librarian, Constants.Roles.Member };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    var result = await _roleManager.CreateAsync(new IdentityRole(role));
                    if (result.Succeeded)
                    {
                        _logger.LogInformation($"Role '{role}' created successfully.");
                    }
                    else
                    {
                        _logger.LogError($"Error creating role '{role}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            const string adminEmail = "admin@librarypro.com";
            const string adminPassword = "Admin@123"; // Change this in production

            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await _userManager.CreateAsync(adminUser, adminPassword);

                if (result.Succeeded)
                {
                    // Assign Admin role
                    await _userManager.AddToRoleAsync(adminUser, Constants.Roles.Admin);
                    _logger.LogInformation($"Admin user '{adminEmail}' created successfully with Admin role.");
                }
                else
                {
                    _logger.LogError($"Error creating admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
            else
            {
                // Ensure admin has Admin role
                if (!await _userManager.IsInRoleAsync(adminUser, Constants.Roles.Admin))
                {
                    await _userManager.AddToRoleAsync(adminUser, Constants.Roles.Admin);
                    _logger.LogInformation($"Admin role assigned to existing user '{adminEmail}'.");
                }
            }
        }
    }
}
