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

            // Skip logging for GET requests (read-only operations) to reduce noise
            // Only log POST, PUT, DELETE, PATCH operations
            if (context.Request.Method == "GET")
            {
                await _next(context);
                return;
            }

            // Skip logging if user is not authenticated (e.g., during seeding)
            if (context.User?.Identity?.IsAuthenticated != true)
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
            var operationType = GetOperationType(context.Request.Method, context.Request.Path);
            var controller = GetControllerName(context) ?? "Unknown";
            var action = GetActionName(context) ?? "Unknown";
            var description = BuildHumanReadableDescription(context, operationType, controller, action);

            return new AuditLog
            {
                UserId = userId,
                UserName = userName,
                Action = context.Request.Method,
                Controller = controller,
                ActionName = action,
                Timestamp = DateTime.UtcNow,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                UserAgent = context.Request.Headers["User-Agent"].ToString(),
                Description = description,
                OperationType = operationType,
                EntityType = GetEntityType(context.Request.Path)
            };
        }

        private string BuildHumanReadableDescription(HttpContext context, string operationType, string controller, string action)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var lowerPath = path.Trim();

            if (lowerPath.Contains("/Account/Login", StringComparison.OrdinalIgnoreCase))
                return "A user signed in to the library system.";

            if (lowerPath.Contains("/Account/Logout", StringComparison.OrdinalIgnoreCase))
                return "A user signed out of the library system.";

            if (lowerPath.Contains("/Account/Register", StringComparison.OrdinalIgnoreCase))
                return "A new member account was registered in the system.";

            if (controller.Equals("Books", StringComparison.OrdinalIgnoreCase))
            {
                if (action.Contains("Create", StringComparison.OrdinalIgnoreCase) || action.Contains("Add", StringComparison.OrdinalIgnoreCase))
                    return "A new book was added to the catalog.";

                if (action.Contains("Edit", StringComparison.OrdinalIgnoreCase) || action.Contains("Update", StringComparison.OrdinalIgnoreCase))
                    return "A book record was updated in the catalog.";

                if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase))
                    return "A book record was removed from the catalog.";

                if (action.Contains("Details", StringComparison.OrdinalIgnoreCase))
                    return "A book record was viewed in the catalog.";

                return "A book operation was performed in the catalog.";
            }

            if (controller.Equals("Members", StringComparison.OrdinalIgnoreCase))
            {
                if (action.Contains("Register", StringComparison.OrdinalIgnoreCase))
                    return "A new member record was registered.";

                if (action.Contains("Edit", StringComparison.OrdinalIgnoreCase) || action.Contains("Update", StringComparison.OrdinalIgnoreCase))
                    return "A member profile was updated.";

                if (action.Contains("Delete", StringComparison.OrdinalIgnoreCase))
                    return "A member record was deleted from the system.";

                if (action.Contains("PayFine", StringComparison.OrdinalIgnoreCase))
                    return "A member fine was cleared in the system.";

                if (action.Contains("Details", StringComparison.OrdinalIgnoreCase))
                    return "A member profile was opened for review.";

                return "A member-related action was performed.";
            }

            if (controller.Equals("Loans", StringComparison.OrdinalIgnoreCase))
            {
                if (action.Contains("Issue", StringComparison.OrdinalIgnoreCase))
                    return "A book was issued to a member.";

                if (action.Contains("Return", StringComparison.OrdinalIgnoreCase))
                    return "A borrowed book was returned to the library.";

                if (action.Contains("Renew", StringComparison.OrdinalIgnoreCase))
                    return "A loan was renewed for a member.";

                return "A lending action was processed.";
            }

            if (controller.Equals("Reservations", StringComparison.OrdinalIgnoreCase))
            {
                if (action.Contains("Create", StringComparison.OrdinalIgnoreCase))
                    return "A book reservation was created for a member.";

                if (action.Contains("Cancel", StringComparison.OrdinalIgnoreCase))
                    return "A book reservation was cancelled.";

                return "A reservation was updated in the queue.";
            }

            if (controller.Equals("Settings", StringComparison.OrdinalIgnoreCase))
            {
                return "Library settings were updated.";
            }

            if (controller.Equals("Reports", StringComparison.OrdinalIgnoreCase))
            {
                return "A report was generated or accessed in the system.";
            }

            if (operationType == "Login")
                return "A user signed in to the library system.";

            if (operationType == "Logout")
                return "A user signed out of the library system.";

            return $"{operationType} activity was recorded in {controller}.";
        }

        private string? GetEntityType(string path)
        {
            var normalized = path.Trim('/');
            if (string.IsNullOrWhiteSpace(normalized))
                return "System";

            var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
                return "System";

            var controller = segments[0];
            return controller switch
            {
                "Books" => "Book",
                "Members" => "Member",
                "Loans" => "BookLoan",
                "Reservations" => "BookReservation",
                "Settings" => "LibrarySettings",
                "Account" => "UserAccount",
                _ => controller
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
