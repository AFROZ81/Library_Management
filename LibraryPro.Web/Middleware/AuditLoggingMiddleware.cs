using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using System.Text.Json;

namespace LibraryPro.Web.Middleware
{
    public class AuditLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<AuditLoggingMiddleware> _logger;

        // Paths to exclude from audit logging
        private readonly string[] _excludePaths = new[]
        {
            "/api/",
            "/health",
            "/metrics",
            "/swagger",
            "/favicon.ico",
            "/css/",
            "/js/",
            "/lib/",
            "/images/",
            "/_framework/",  // Blazor framework files
            "/_blazor",     // Blazor signalr
            "/signalr",     // SignalR connections
            "/connect"      // SignalR connections
        };

        public AuditLoggingMiddleware(
            RequestDelegate next,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<AuditLoggingMiddleware> logger)
        {
            _next = next;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip logging for excluded paths
            if (ShouldExcludePath(context.Request.Path))
            {
                await _next(context);
                return;
            }

            // Skip logging for static files
            if (context.Request.Path.StartsWithSegments("/wwwroot") || 
                context.Request.Path.StartsWithSegments("/images") ||
                context.Request.Path.StartsWithSegments("/css") ||
                context.Request.Path.StartsWithSegments("/js"))
            {
                await _next(context);
                return;
            }

            var auditLog = CreateAuditLog(context);

            try
            {
                await _next(context);

                // Update audit log with response status
                auditLog.IsSuccess = context.Response.StatusCode < 400;
                if (!auditLog.IsSuccess)
                {
                    auditLog.ErrorMessage = $"HTTP {context.Response.StatusCode}";
                }
            }
            catch (Exception ex)
            {
                auditLog.IsSuccess = false;
                auditLog.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Error during request processing for audit logging");
                throw;
            }
            finally
            {
                // Save audit log asynchronously in background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                        await auditLogRepository.AddAsync(auditLog);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving audit log");
                    }
                });
            }
        }

        private AuditLog CreateAuditLog(HttpContext context)
        {
            var user = context.User;
            var userId = user?.Identity?.IsAuthenticated == true ? user.Identity.Name : null;
            var userName = user?.Identity?.Name;

            return new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = context.Request.Method,
                Controller = GetControllerName(context),
                ActionName = GetActionName(context),
                Timestamp = DateTime.UtcNow,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Description = $"{context.Request.Method} {context.Request.Path}",
                OperationType = GetOperationType(context.Request.Method, context.Request.Path)
            };
        }

        private bool ShouldExcludePath(string path)
        {
            return _excludePaths.Any(excludePath => path.StartsWith(excludePath, StringComparison.OrdinalIgnoreCase));
        }

        private string? GetControllerName(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var routePattern = endpoint.DisplayName;
                if (routePattern != null)
                {
                    var parts = routePattern.Split('/');
                    if (parts.Length >= 2)
                    {
                        return parts[1];
                    }
                }
            }
            // Fallback: try to extract from request path
            var pathParts = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts != null && pathParts.Length >= 1)
            {
                return pathParts[0];
            }
            return "Unknown";
        }

        private string? GetActionName(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint != null)
            {
                var routePattern = endpoint.DisplayName;
                if (routePattern != null)
                {
                    var parts = routePattern.Split('/');
                    if (parts.Length >= 3)
                    {
                        return parts[2];
                    }
                }
            }
            // Fallback: try to extract from request path
            var pathParts = context.Request.Path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (pathParts != null && pathParts.Length >= 2)
            {
                return pathParts[1];
            }
            return "Unknown";
        }

        private string GetOperationType(string method, string path)
        {
            // Determine operation type based on HTTP method and path
            if (path.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                return "Login";
            if (path.Contains("/Account/Logout", StringComparison.OrdinalIgnoreCase))
                return "Logout";
            if (path.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
                return "Register";

            return method switch
            {
                "GET" => "Read",
                "POST" => "Create",
                "PUT" => "Update",
                "PATCH" => "Update",
                "DELETE" => "Delete",
                _ => "Unknown"
            };
        }
    }

    // Extension method for easy middleware registration
    public static class AuditLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuditLoggingMiddleware>();
        }
    }
}
