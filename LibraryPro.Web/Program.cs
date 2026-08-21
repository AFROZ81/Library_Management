using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // User settings
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Add authorization policies
builder.Services.AddAuthorization(options =>
{
    // Admin policy - Full access to all features
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Constants.Roles.Admin));
    
    // Librarian policy - Book management, loan management, member management
    options.AddPolicy("LibrarianOrAdmin", policy => 
        policy.RequireRole(Constants.Roles.Librarian, Constants.Roles.Admin));
    
    // Member policy - View catalog, personal loans, reservations
    options.AddPolicy("MemberOrAbove", policy => 
        policy.RequireRole(Constants.Roles.Member, Constants.Roles.Librarian, Constants.Roles.Admin));
});

// Configure authentication
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<IBookRepository, BookRepository>();

builder.Services.AddScoped<IMemberRepository, MemberRepository>();

builder.Services.AddScoped<ILoanRepository, LoanRepository>();

// Register DatabaseSeeder
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Seed database on startup
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();


app.Run();
