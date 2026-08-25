using LibraryPro.Web.Repositories;

namespace LibraryPro.Web.Services
{
    public class AuditLogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditLogCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromDays(1); // Run daily

        public AuditLogCleanupService(
            IServiceProvider serviceProvider,
            ILogger<AuditLogCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit Log Cleanup Service started.");

            // Run immediately on startup, then on schedule
            await CleanupOldLogsAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken);
                    await CleanupOldLogsAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Shutdown requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in audit log cleanup service");
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Retry after 1 hour
                }
            }

            _logger.LogInformation("Audit Log Cleanup Service stopped.");
        }

        private async Task CleanupOldLogsAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var auditLogRepository = scope.ServiceProvider.GetRequiredService<IAuditLogRepository>();
                
                // Default retention: 90 days (can be made configurable)
                var retentionDays = 90;
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                
                await auditLogRepository.DeleteOldLogsAsync(cutoffDate);
                
                _logger.LogInformation("Audit log cleanup completed. Deleted logs older than {RetentionDays} days", retentionDays);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log cleanup");
            }
        }
    }
}
