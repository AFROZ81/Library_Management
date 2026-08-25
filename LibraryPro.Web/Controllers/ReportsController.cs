using LibraryPro.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Controllers
{
    [Authorize(Policy = "LibrarianOrAdmin")]
    public class ReportsController : Controller
    {
        private readonly IReportService _reportService;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(IReportService reportService, ILogger<ReportsController> logger)
        {
            _reportService = reportService;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Circulation Reports
        public IActionResult Circulation()
        {
            var model = new ReportDateRangeViewModel
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Circulation(ReportDateRangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var data = await _reportService.GetCirculationReportDataAsync(model.StartDate, model.EndDate);
                return View("CirculationResult", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating circulation report");
                TempData["Error"] = "Error generating report. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> CirculationPdf(DateTime startDate, DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateCirculationReportPdfAsync(startDate, endDate);
                return File(pdfBytes, "application/pdf", $"Circulation_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating circulation PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(Circulation));
            }
        }

        public async Task<IActionResult> CirculationExcel(DateTime startDate, DateTime endDate)
        {
            try
            {
                var excelBytes = await _reportService.GenerateCirculationReportExcelAsync(startDate, endDate);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Circulation_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating circulation Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(Circulation));
            }
        }

        // Financial Reports
        public IActionResult Financial()
        {
            var model = new ReportDateRangeViewModel
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Financial(ReportDateRangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var data = await _reportService.GetFinancialReportDataAsync(model.StartDate, model.EndDate);
                return View("FinancialResult", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial report");
                TempData["Error"] = "Error generating report. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> FinancialPdf(DateTime startDate, DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateFinancialReportPdfAsync(startDate, endDate);
                return File(pdfBytes, "application/pdf", $"Financial_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(Financial));
            }
        }

        public async Task<IActionResult> FinancialExcel(DateTime startDate, DateTime endDate)
        {
            try
            {
                var excelBytes = await _reportService.GenerateFinancialReportExcelAsync(startDate, endDate);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Financial_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(Financial));
            }
        }

        // Popular Books Reports
        public IActionResult PopularBooks()
        {
            var model = new PopularBooksReportViewModel
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow,
                TopN = 20
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> PopularBooks(PopularBooksReportViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var data = await _reportService.GetPopularBooksReportDataAsync(model.StartDate, model.EndDate, model.TopN);
                return View("PopularBooksResult", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating popular books report");
                TempData["Error"] = "Error generating report. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> PopularBooksPdf(DateTime startDate, DateTime endDate, int topN = 20)
        {
            try
            {
                var pdfBytes = await _reportService.GeneratePopularBooksReportPdfAsync(startDate, endDate, topN);
                return File(pdfBytes, "application/pdf", $"Popular_Books_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating popular books PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(PopularBooks));
            }
        }

        public async Task<IActionResult> PopularBooksExcel(DateTime startDate, DateTime endDate, int topN = 20)
        {
            try
            {
                var excelBytes = await _reportService.GeneratePopularBooksReportExcelAsync(startDate, endDate, topN);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Popular_Books_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating popular books Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(PopularBooks));
            }
        }

        // Member Activity Reports
        public IActionResult MemberActivity()
        {
            var model = new ReportDateRangeViewModel
            {
                StartDate = DateTime.UtcNow.AddDays(-30),
                EndDate = DateTime.UtcNow
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> MemberActivity(ReportDateRangeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var data = await _reportService.GetMemberActivityReportDataAsync(model.StartDate, model.EndDate);
                return View("MemberActivityResult", data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating member activity report");
                TempData["Error"] = "Error generating report. Please try again.";
                return View(model);
            }
        }

        public async Task<IActionResult> MemberActivityPdf(DateTime startDate, DateTime endDate)
        {
            try
            {
                var pdfBytes = await _reportService.GenerateMemberActivityReportPdfAsync(startDate, endDate);
                return File(pdfBytes, "application/pdf", $"Member_Activity_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating member activity PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(MemberActivity));
            }
        }

        public async Task<IActionResult> MemberActivityExcel(DateTime startDate, DateTime endDate)
        {
            try
            {
                var excelBytes = await _reportService.GenerateMemberActivityReportExcelAsync(startDate, endDate);
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Member_Activity_Report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating member activity Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(MemberActivity));
            }
        }

        // Overdue Books Report
        public async Task<IActionResult> OverdueBooks()
        {
            try
            {
                var data = await _reportService.GetOverdueBooksReportDataAsync();
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overdue books report");
                TempData["Error"] = "Error generating report. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> OverdueBooksPdf()
        {
            try
            {
                var pdfBytes = await _reportService.GenerateOverdueBooksReportPdfAsync();
                return File(pdfBytes, "application/pdf", $"Overdue_Books_Report_{DateTime.UtcNow:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overdue books PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(OverdueBooks));
            }
        }

        public async Task<IActionResult> OverdueBooksExcel()
        {
            try
            {
                var excelBytes = await _reportService.GenerateOverdueBooksReportExcelAsync();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Overdue_Books_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overdue books Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(OverdueBooks));
            }
        }

        // Inventory Status Report
        public async Task<IActionResult> Inventory()
        {
            try
            {
                var data = await _reportService.GetInventoryReportDataAsync();
                return View(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory report");
                TempData["Error"] = "Error generating report. Please try again.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> InventoryPdf()
        {
            try
            {
                var pdfBytes = await _reportService.GenerateInventoryReportPdfAsync();
                return File(pdfBytes, "application/pdf", $"Inventory_Report_{DateTime.UtcNow:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory PDF");
                TempData["Error"] = "Error generating PDF. Please try again.";
                return RedirectToAction(nameof(Inventory));
            }
        }

        public async Task<IActionResult> InventoryExcel()
        {
            try
            {
                var excelBytes = await _reportService.GenerateInventoryReportExcelAsync();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"Inventory_Report_{DateTime.UtcNow:yyyyMMdd}.xlsx");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory Excel");
                TempData["Error"] = "Error generating Excel. Please try again.";
                return RedirectToAction(nameof(Inventory));
            }
        }
    }

    // View Models for Controller
    public class ReportDateRangeViewModel
    {
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
    }

    public class PopularBooksReportViewModel : ReportDateRangeViewModel
    {
        [Range(1, 100)]
        public int TopN { get; set; } = 20;
    }
}
