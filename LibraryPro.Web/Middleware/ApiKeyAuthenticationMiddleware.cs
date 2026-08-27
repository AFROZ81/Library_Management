using LibraryPro.Web.Repositories;
using System.Net;

namespace LibraryPro.Web.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(
        RequestDelegate next,
        IServiceScopeFactory scopeFactory,
        ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip API key authentication for Swagger and non-API routes
        if (context.Request.Path.StartsWithSegments("/swagger") || 
            !context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Check for API key in header
        if (!context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyValue) || string.IsNullOrEmpty(apiKeyValue))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API key is required");
            return;
        }

        // Create a scope to resolve scoped services
        using var scope = _scopeFactory.CreateScope();
        var apiKeyRepository = scope.ServiceProvider.GetRequiredService<IApiKeyRepository>();

        var apiKey = await apiKeyRepository.GetByKeyAsync(apiKeyValue.ToString() ?? string.Empty);
        if (apiKey == null)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Invalid API key");
            return;
        }

        if (!apiKey.IsActive)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API key is inactive");
            return;
        }

        if (apiKey.ExpiresAt.HasValue && apiKey.ExpiresAt.Value < DateTime.UtcNow)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("API key has expired");
            return;
        }

        // Increment usage count
        await apiKeyRepository.IncrementUsageAsync(apiKey.Id);

        // Add API key info to context
        context.Items["ApiKey"] = apiKey;

        await _next(context);
    }
}
