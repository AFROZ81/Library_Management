using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    public class AuditController : Controller
    {
        private readonly IAuditLogRepository _auditLogRepository;
        private readonly ILogger<AuditController> _logger;

        public AuditController(IAuditLogRepository auditLogRepository, ILogger<AuditController> logger)
        {
            _auditLogRepository = auditLogRepository;
            _logger = logger;
        }

        public async Task<IActionResult> Index(
            string? operationType,
            string? entityType,
            DateTime? startDate,
            DateTime? endDate,
            int pageNumber = 1)
        {
            const int pageSize = 50;
            
            // Reset filters if all are empty (default to "All")
            if (string.IsNullOrEmpty(operationType) && string.IsNullOrEmpty(entityType) && !startDate.HasValue && !endDate.HasValue)
            {
                operationType = null;
                entityType = null;
                startDate = null;
                endDate = null;
            }
            
            IEnumerable<AuditLog> logs;

            // Apply filters
            if (!string.IsNullOrEmpty(operationType))
            {
                logs = await _auditLogRepository.GetByOperationTypeAsync(operationType);
            }
            else if (!string.IsNullOrEmpty(entityType))
            {
                logs = await _auditLogRepository.GetByEntityTypeAsync(entityType);
            }
            else if (startDate.HasValue && endDate.HasValue)
            {
                logs = await _auditLogRepository.GetByDateRangeAsync(startDate.Value, endDate.Value);
            }
            else
            {
                logs = await _auditLogRepository.GetAllAsync();
            }

            // Pagination
            var totalLogs = logs.Count();
            var paginatedLogs = logs
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var viewModel = new AuditLogIndexViewModel
            {
                AuditLogs = paginatedLogs,
                CurrentPage = pageNumber,
                TotalPages = (int)Math.Ceiling(totalLogs / (double)pageSize),
                OperationType = operationType,
                EntityType = entityType,
                StartDate = startDate,
                EndDate = endDate
            };

            // Pass filter options to view
            ViewData["OperationTypes"] = new List<string> { "Create", "Read", "Update", "Delete", "Login", "Logout", "Register" };
            ViewData["EntityTypes"] = new List<string> { "Book", "Member", "BookLoan", "FinePayment", "LibrarySettings" };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var log = await _auditLogRepository.GetByIdAsync(id);
            if (log == null)
            {
                return NotFound();
            }

            return View(log);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearOldLogs(int retentionDays)
        {
            if (retentionDays < 30)
            {
                TempData["Error"] = "Retention period must be at least 30 days.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);
                var deletedCount = await _auditLogRepository.DeleteOldLogsAsync(cutoffDate);
                
                if (deletedCount > 0)
                {
                    TempData["Success"] = $"Deleted {deletedCount} audit logs older than {retentionDays} days.";
                    _logger.LogInformation("Admin cleared {Count} audit logs older than {RetentionDays} days", deletedCount, retentionDays);
                }
                else
                {
                    TempData["Error"] = $"No audit logs found older than {retentionDays} days. The oldest log is newer than the cutoff date.";
                    _logger.LogWarning("No audit logs deleted. Cutoff date: {CutoffDate}, Retention days: {RetentionDays}", cutoffDate, retentionDays);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing old audit logs");
                TempData["Error"] = "Error clearing old audit logs. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }
    }

    public class AuditLogIndexViewModel
    {
        public List<AuditLog> AuditLogs { get; set; } = new();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? OperationType { get; set; }
        public string? EntityType { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
