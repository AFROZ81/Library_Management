using LibraryPro.Web.Data;
using LibraryPro.Web.Models;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Services;
using LibraryPro.Web.Middleware;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<ApplicationDbContext>(options => 
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(Constants.Roles.Admin));
    options.AddPolicy("LibrarianOrAdmin", policy => 
        policy.RequireRole(Constants.Roles.Librarian, Constants.Roles.Admin));
    options.AddPolicy("MemberOrAbove", policy => 
        policy.RequireRole(Constants.Roles.Member, Constants.Roles.Librarian, Constants.Roles.Admin));
});

// Configure authentication cookies
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
});

// Register Core Repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<ILibrarySettingsRepository, LibrarySettingsRepository>();
builder.Services.AddScoped<IBookReservationRepository, BookReservationRepository>();
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IApiKeyRepository, ApiKeyRepository>();

// Register Email Services
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddSingleton<EmailQueue>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IEmailLogRepository, EmailLogRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddHostedService<EmailBackgroundService>();
builder.Services.AddHostedService<AuditLogCleanupService>();

// Register Image Service
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection("ImageSettings"));
builder.Services.AddScoped<IImageService, ImageService>();

// Register Report Service
builder.Services.AddScoped<IReportService, ReportService>();

// Register Recommendation Service
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// Register Barcode and QR Code Services
builder.Services.AddScoped<IBarcodeService, BarcodeService>();
builder.Services.AddScoped<IQRCodeService, QRCodeService>();

// Register External Book Services
builder.Services.AddHttpClient<GoogleBooksService>();
builder.Services.AddScoped<IExternalBookService, GoogleBooksService>();
builder.Services.AddHttpClient<OpenLibraryService>();
builder.Services.AddScoped<OpenLibraryService>();

// Register DatabaseSeeder
builder.Services.AddScoped<DatabaseSeeder>();

// Add Swagger/OpenAPI services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add Rate Limiting (simplified)
builder.Services.AddRateLimiter();

// Add JWT Authentication (for API endpoints only)
builder.Services.AddAuthentication()
.AddJwtBearer("Bearer", options =>
{
    options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
            System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "YourSecretKeyHere12345678901234567890"))
    };
});

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
    app.UseHsts();
}

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraryPro API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseRouting();

// Apply rate limiting to API endpoints
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Add audit logging middleware
app.UseAuditLogging();

// Add API key authentication middleware
app.UseMiddleware<LibraryPro.Web.Middleware.ApiKeyAuthenticationMiddleware>();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}")
    .WithStaticAssets();

app.Run();
