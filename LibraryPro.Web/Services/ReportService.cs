using ClosedXML.Excel;
using LibraryPro.Web.Data;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Models;
using Microsoft.EntityFrameworkCore;
using PdfSharpCore.Pdf;
using PdfSharpCore.Drawing;
using System.IO;

namespace LibraryPro.Web.Services
{
    public class ReportService : IReportService
    {

        private readonly ILoanRepository _loanRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportService> _logger;

        public ReportService(
            ILoanRepository loanRepository,
            IBookRepository bookRepository,
            IMemberRepository memberRepository,
            ApplicationDbContext context,
            ILogger<ReportService> logger)
        {
            _loanRepository = loanRepository;
            _bookRepository = bookRepository;
            _memberRepository = memberRepository;
            _context = context;
            _logger = logger;
        }

        // Circulation Reports
        public async Task<CirculationReportViewModel> GetCirculationReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var filteredLoans = allLoans
                .Where(l => l.LoanDate >= startDate && l.LoanDate <= endDate)
                .ToList();

            var dailyCirculation = filteredLoans
                .GroupBy(l => l.LoanDate.Date)
                .Select(g => new DailyCirculation
                {
                    Date = g.Key,
                    LoansIssued = g.Count(),
                    LoansReturned = g.Count(l => l.IsReturned)
                })
                .OrderBy(d => d.Date)
                .ToList();

            var returnedLoans = filteredLoans
                .Where(l => l.IsReturned && l.ReturnDate.HasValue)
                .ToList();

            var averageLoanDuration = returnedLoans.Any()
                ? returnedLoans.Average(l => (l.ReturnDate!.Value - l.LoanDate).TotalDays)
                : 0;

            return new CirculationReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalLoansIssued = filteredLoans.Count,
                TotalLoansReturned = filteredLoans.Count(l => l.IsReturned),
                ActiveLoans = filteredLoans.Count(l => !l.IsReturned),
                AverageLoanDuration = averageLoanDuration,
                DailyCirculation = dailyCirculation
            };
        }

        public async Task<byte[]> GenerateCirculationReportPdfAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for circulation report from {StartDate} to {EndDate}", startDate, endDate);
                var data = await GetCirculationReportDataAsync(startDate, endDate);
                _logger.LogInformation("Retrieved circulation data: {TotalLoans} loans", data.TotalLoansIssued);

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Circulation Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // String formats for alignment
                var formatLeft = new XStringFormat();
                formatLeft.Alignment = XStringAlignment.Near;
                formatLeft.LineAlignment = XLineAlignment.Center;
                
                var formatRight = new XStringFormat();
                formatRight.Alignment = XStringAlignment.Far;
                formatRight.LineAlignment = XLineAlignment.Center;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Circulation Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Summary section
                int y = margin + 100;
                var summaryBoxHeight = 120;
                
                // Draw summary box
                graphics.DrawRectangle(XBrushes.White, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                graphics.DrawRectangle(colorBorderDark, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                
                // Summary title
                graphics.DrawString("Summary", fontSubtitle, colorPrimary, margin + 10, y + 10);
                
                // Summary data in grid
                var summaryY = y + 40;
                var colWidth = (pageWidth - 2 * margin - 20) / 2;
                
                graphics.DrawString($"Total Loans Issued:", fontHeader, colorSecondary, margin + 10, summaryY, formatLeft);
                graphics.DrawString(data.TotalLoansIssued.ToString(), fontHeader, XBrushes.Black, margin + 10 + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Total Loans Returned:", fontHeader, colorSecondary, margin + 10 + colWidth, summaryY, formatLeft);
                graphics.DrawString(data.TotalLoansReturned.ToString(), fontHeader, XBrushes.Black, margin + 10 + colWidth + colWidth - 10, summaryY, formatRight);
                
                summaryY += 30;
                graphics.DrawString($"Active Loans:", fontHeader, colorSecondary, margin + 10, summaryY, formatLeft);
                graphics.DrawString(data.ActiveLoans.ToString(), fontHeader, XBrushes.Blue, margin + 10 + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Avg Duration (days):", fontHeader, colorSecondary, margin + 10 + colWidth, summaryY, formatLeft);
                graphics.DrawString($"{data.AverageLoanDuration:F1}", fontHeader, XBrushes.Black, margin + 10 + colWidth + colWidth - 10, summaryY, formatRight);

                y += summaryBoxHeight + 20;
                
                // Table section
                graphics.DrawString("Daily Circulation", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 120;
                var col2Width = 100;
                var col3Width = 100;
                var tableWidth = col1Width + col2Width + col3Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text with proper alignment
                var headerX = margin + 5;
                graphics.DrawString("Date", fontHeader, XBrushes.Black, headerX, tableY + 8, formatLeft);
                graphics.DrawString("Issued", fontHeader, XBrushes.Black, headerX + col1Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Returned", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + 5, tableY + 8, formatLeft);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                
                foreach (var daily in data.DailyCirculation)
                {
                    // Alternate row colors
                    if (rowCount % 2 == 0)
                    {
                        graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                    }
                    
                    // Draw row border
                    graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                    
                    // Row text with proper alignment
                    graphics.DrawString(daily.Date.ToString("dd MMM yyyy"), fontNormal, XBrushes.Black, headerX, y + 8, formatLeft);
                    graphics.DrawString(daily.LoansIssued.ToString(), fontNormal, XBrushes.Black, headerX + col1Width + 5, y + 8, formatLeft);
                    graphics.DrawString(daily.LoansReturned.ToString(), fontNormal, XBrushes.Black, headerX + col1Width + col2Width + 5, y + 8, formatLeft);
                    
                    y += rowHeight;
                    rowCount++;
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10, formatLeft);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating circulation PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateCirculationReportExcelAsync(DateTime startDate, DateTime endDate)
        {
            var data = await GetCirculationReportDataAsync(startDate, endDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Circulation Report");

            // Title
            worksheet.Cell("A1").Value = "LibraryPro - Circulation Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            // Period
            worksheet.Cell("A2").Value = $"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";

            // Summary
            worksheet.Cell("A4").Value = "Summary";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A5").Value = "Total Loans Issued:";
            worksheet.Cell("B5").Value = data.TotalLoansIssued;
            worksheet.Cell("A6").Value = "Total Loans Returned:";
            worksheet.Cell("B6").Value = data.TotalLoansReturned;
            worksheet.Cell("A7").Value = "Active Loans:";
            worksheet.Cell("B7").Value = data.ActiveLoans;
            worksheet.Cell("A8").Value = "Average Duration (days):";
            worksheet.Cell("B8").Value = data.AverageLoanDuration.ToString("F1");

            // Daily Circulation Table
            worksheet.Cell("A10").Value = "Daily Circulation";
            worksheet.Cell("A10").Style.Font.Bold = true;

            worksheet.Cell("A11").Value = "Date";
            worksheet.Cell("B11").Value = "Issued";
            worksheet.Cell("C11").Value = "Returned";
            worksheet.Range("A11:C11").Style.Font.Bold = true;
            worksheet.Range("A11:C11").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 12;
            foreach (var daily in data.DailyCirculation)
            {
                worksheet.Cell(row, 1).Value = daily.Date.ToString("dd MMM yyyy");
                worksheet.Cell(row, 2).Value = daily.LoansIssued;
                worksheet.Cell(row, 3).Value = daily.LoansReturned;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Financial Reports
        public async Task<FinancialReportViewModel> GetFinancialReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var members = await _memberRepository.GetAllAsync();
            var finePayments = await _context.FinePayments.ToListAsync();

            var payments = new List<PaymentRecord>();

            foreach (var payment in finePayments)
            {
                if (payment.PaymentDate >= startDate && payment.PaymentDate <= endDate)
                {
                    var member = members.FirstOrDefault(m => m.Id == payment.MemberId);
                    payments.Add(new PaymentRecord
                    {
                        PaymentDate = payment.PaymentDate,
                        MemberName = member?.Name ?? "Unknown",
                        Amount = payment.Amount,
                        PaymentMethod = "Cash"
                    });
                }
            }

            return new FinancialReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalFinesCollected = payments.Sum(p => p.Amount),
                TotalPayments = payments.Count,
                AveragePaymentAmount = payments.Any() ? payments.Average(p => p.Amount) : 0,
                Payments = payments.OrderByDescending(p => p.PaymentDate).ToList()
            };
        }

        public async Task<byte[]> GenerateFinancialReportPdfAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for financial report from {StartDate} to {EndDate}", startDate, endDate);
                var data = await GetFinancialReportDataAsync(startDate, endDate);
                _logger.LogInformation("Retrieved financial data: {TotalPayments} payments", data.TotalPayments);

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Financial Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // String formats for alignment
                var formatLeft = new XStringFormat();
                formatLeft.Alignment = XStringAlignment.Near;
                formatLeft.LineAlignment = XLineAlignment.Center;
                
                var formatRight = new XStringFormat();
                formatRight.Alignment = XStringAlignment.Far;
                formatRight.LineAlignment = XLineAlignment.Center;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Financial Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Summary section
                int y = margin + 100;
                var summaryBoxHeight = 100;
                
                // Draw summary box
                graphics.DrawRectangle(XBrushes.White, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                graphics.DrawRectangle(colorBorderDark, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                
                // Summary title
                graphics.DrawString("Summary", fontSubtitle, colorPrimary, margin + 10, y + 10);
                
                // Summary data in grid
                var summaryY = y + 40;
                var colWidth = (pageWidth - 2 * margin - 20) / 3;
                
                graphics.DrawString($"Total Fines Collected:", fontHeader, colorSecondary, margin + 10, summaryY, formatLeft);
                graphics.DrawString($"₹{data.TotalFinesCollected:F2}", fontHeader, XBrushes.Green, margin + 10 + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Total Payments:", fontHeader, colorSecondary, margin + 10 + colWidth, summaryY, formatLeft);
                graphics.DrawString(data.TotalPayments.ToString(), fontHeader, XBrushes.Black, margin + 10 + colWidth + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Average Payment:", fontHeader, colorSecondary, margin + 10 + colWidth * 2, summaryY, formatLeft);
                graphics.DrawString($"₹{data.AveragePaymentAmount:F2}", fontHeader, XBrushes.Black, margin + 10 + colWidth * 2 + colWidth - 10, summaryY, formatRight);

                y += summaryBoxHeight + 20;
                
                // Table section
                graphics.DrawString("Payment Records", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 100;
                var col2Width = 200;
                var col3Width = 80;
                var tableWidth = col1Width + col2Width + col3Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text with proper alignment
                var headerX = margin + 5;
                graphics.DrawString("Date", fontHeader, XBrushes.Black, headerX, tableY + 8, formatLeft);
                graphics.DrawString("Member", fontHeader, XBrushes.Black, headerX + col1Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Amount", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + 5, tableY + 8, formatLeft);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                
                foreach (var payment in data.Payments.Take(50))
                {
                    // Alternate row colors
                    if (rowCount % 2 == 0)
                    {
                        graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                    }
                    
                    // Draw row border
                    graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                    
                    // Row text with proper alignment
                    graphics.DrawString(payment.PaymentDate.ToString("dd MMM yyyy"), fontNormal, XBrushes.Black, headerX, y + 8, formatLeft);
                    graphics.DrawString(payment.MemberName, fontNormal, XBrushes.Black, headerX + col1Width + 5, y + 8, formatLeft);
                    graphics.DrawString($"₹{payment.Amount:F2}", fontNormal, XBrushes.Green, headerX + col1Width + col2Width + 5, y + 8, formatLeft);
                    
                    y += rowHeight;
                    rowCount++;
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10, formatLeft);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating financial PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateFinancialReportExcelAsync(DateTime startDate, DateTime endDate)
        {
            var data = await GetFinancialReportDataAsync(startDate, endDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Financial Report");

            worksheet.Cell("A1").Value = "LibraryPro - Financial Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A2").Value = $"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";

            worksheet.Cell("A4").Value = "Summary";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A5").Value = "Total Fines Collected:";
            worksheet.Cell("B5").Value = data.TotalFinesCollected;
            worksheet.Cell("A6").Value = "Total Payments:";
            worksheet.Cell("B6").Value = data.TotalPayments;
            worksheet.Cell("A7").Value = "Average Payment:";
            worksheet.Cell("B7").Value = data.AveragePaymentAmount;

            worksheet.Cell("A9").Value = "Payment Records";
            worksheet.Cell("A9").Style.Font.Bold = true;

            worksheet.Cell("A10").Value = "Date";
            worksheet.Cell("B10").Value = "Member";
            worksheet.Cell("C10").Value = "Amount";
            worksheet.Range("A10:C10").Style.Font.Bold = true;
            worksheet.Range("A10:C10").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 11;
            foreach (var payment in data.Payments)
            {
                worksheet.Cell(row, 1).Value = payment.PaymentDate.ToString("dd MMM yyyy");
                worksheet.Cell(row, 2).Value = payment.MemberName;
                worksheet.Cell(row, 3).Value = payment.Amount;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Popular Books Reports
        public async Task<PopularBooksReportViewModel> GetPopularBooksReportDataAsync(DateTime startDate, DateTime endDate, int topN = 20)
        {
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var books = await _bookRepository.GetAllAsync();

            var popularBooks = allLoans
                .Where(l => l.LoanDate >= startDate && l.LoanDate <= endDate)
                .GroupBy(l => l.BookId)
                .Select(g => new PopularBook
                {
                    BookId = g.Key,
                    TimesBorrowed = g.Count(),
                    Title = books.FirstOrDefault(b => b.Id == g.Key)?.Title ?? "Unknown",
                    Author = books.FirstOrDefault(b => b.Id == g.Key)?.Author ?? "Unknown",
                    Genre = books.FirstOrDefault(b => b.Id == g.Key)?.Genre ?? new List<string>()
                })
                .OrderByDescending(b => b.TimesBorrowed)
                .Take(topN)
                .ToList();

            return new PopularBooksReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                PopularBooks = popularBooks
            };
        }

        public async Task<byte[]> GeneratePopularBooksReportPdfAsync(DateTime startDate, DateTime endDate, int topN = 20)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for popular books report from {StartDate} to {EndDate}", startDate, endDate);
                var data = await GetPopularBooksReportDataAsync(startDate, endDate, topN);
                _logger.LogInformation("Retrieved popular books data: {TotalBooks} books", data.PopularBooks.Count);

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Popular Books Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // String formats for alignment
                var formatLeft = new XStringFormat();
                formatLeft.Alignment = XStringAlignment.Near;
                formatLeft.LineAlignment = XLineAlignment.Center;
                
                var formatRight = new XStringFormat();
                formatRight.Alignment = XStringAlignment.Far;
                formatRight.LineAlignment = XLineAlignment.Center;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Popular Books Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Table section
                int y = margin + 100;
                graphics.DrawString($"Top {data.PopularBooks.Count} Most Borrowed Books", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 40;
                var col2Width = 220;
                var col3Width = 220;
                var col4Width = 60;
                var tableWidth = col1Width + col2Width + col3Width + col4Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text with proper alignment
                var headerX = margin + 5;
                graphics.DrawString("#", fontHeader, XBrushes.Black, headerX, tableY + 8, formatLeft);
                graphics.DrawString("Title", fontHeader, XBrushes.Black, headerX + col1Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Author", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Borrowed", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + col3Width + 5, tableY + 8, formatLeft);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                int rank = 1;
                
                foreach (var book in data.PopularBooks)
                {
                    // Alternate row colors
                    if (rowCount % 2 == 0)
                    {
                        graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                    }
                    
                    // Draw row border
                    graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                    
                    // Row text with proper alignment
                    graphics.DrawString(rank.ToString(), fontNormal, XBrushes.Black, headerX, y + 8, formatLeft);
                    graphics.DrawString(book.Title, fontNormal, XBrushes.Black, headerX + col1Width + 5, y + 8, formatLeft);
                    graphics.DrawString(book.Author, fontNormal, XBrushes.Black, headerX + col1Width + col2Width + 5, y + 8, formatLeft);
                    graphics.DrawString(book.TimesBorrowed.ToString(), fontNormal, XBrushes.Blue, headerX + col1Width + col2Width + col3Width + 5, y + 8, formatLeft);
                    
                    y += rowHeight;
                    rowCount++;
                    rank++;
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10, formatLeft);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating popular books PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GeneratePopularBooksReportExcelAsync(DateTime startDate, DateTime endDate, int topN = 20)
        {
            var data = await GetPopularBooksReportDataAsync(startDate, endDate, topN);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Popular Books");

            worksheet.Cell("A1").Value = "LibraryPro - Popular Books Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A2").Value = $"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";

            worksheet.Cell("A4").Value = "Top Most Borrowed Books";
            worksheet.Cell("A4").Style.Font.Bold = true;

            worksheet.Cell("A5").Value = "#";
            worksheet.Cell("B5").Value = "Title";
            worksheet.Cell("C5").Value = "Author";
            worksheet.Cell("D5").Value = "Times Borrowed";
            worksheet.Range("A5:D5").Style.Font.Bold = true;
            worksheet.Range("A5:D5").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 6;
            int rank = 1;
            foreach (var book in data.PopularBooks)
            {
                worksheet.Cell(row, 1).Value = rank;
                worksheet.Cell(row, 2).Value = book.Title;
                worksheet.Cell(row, 3).Value = book.Author;
                worksheet.Cell(row, 4).Value = book.TimesBorrowed;
                row++;
                rank++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Member Activity Reports
        public async Task<MemberActivityReportViewModel> GetMemberActivityReportDataAsync(DateTime startDate, DateTime endDate)
        {
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var members = await _memberRepository.GetAllAsync();
            var finePayments = await _context.FinePayments.ToListAsync();

            var memberActivities = members.Select(member =>
            {
                var memberLoans = allLoans.Where(l => l.MemberId == member.Id && l.LoanDate >= startDate && l.LoanDate <= endDate);
                var totalFinesPaid = finePayments
                    .Where(fp => fp.MemberId == member.Id && fp.PaymentDate >= startDate && fp.PaymentDate <= endDate)
                    .Sum(fp => fp.Amount);

                return new MemberActivity
                {
                    MemberId = member.Id,
                    MemberName = member.Name,
                    Email = member.Email,
                    BooksBorrowed = memberLoans.Count(),
                    TotalFinesPaid = totalFinesPaid
                };
            })
            .Where(m => m.BooksBorrowed > 0)
            .OrderByDescending(m => m.BooksBorrowed)
            .ToList();

            return new MemberActivityReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                ActiveMembers = memberActivities.Count,
                MemberActivities = memberActivities
            };
        }

        public async Task<byte[]> GenerateMemberActivityReportPdfAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for member activity report from {StartDate} to {EndDate}", startDate, endDate);
                var data = await GetMemberActivityReportDataAsync(startDate, endDate);
                _logger.LogInformation("Retrieved member activity data: {TotalMembers} members", data.ActiveMembers);

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Member Activity Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // String formats for alignment
                var formatLeft = new XStringFormat();
                formatLeft.Alignment = XStringAlignment.Near;
                formatLeft.LineAlignment = XLineAlignment.Center;
                
                var formatRight = new XStringFormat();
                formatRight.Alignment = XStringAlignment.Far;
                formatRight.LineAlignment = XLineAlignment.Center;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Member Activity Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Summary section
                int y = margin + 100;
                var summaryBoxHeight = 80;
                
                // Draw summary box
                graphics.DrawRectangle(XBrushes.White, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                graphics.DrawRectangle(colorBorderDark, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                
                // Summary title
                graphics.DrawString("Summary", fontSubtitle, colorPrimary, margin + 10, y + 10);
                
                // Summary data
                var summaryY = y + 40;
                graphics.DrawString($"Active Members:", fontHeader, colorSecondary, margin + 10, summaryY, formatLeft);
                graphics.DrawString(data.ActiveMembers.ToString(), fontHeader, XBrushes.Blue, margin + 10 + 150, summaryY, formatLeft);

                y += summaryBoxHeight + 20;
                
                // Table section
                graphics.DrawString("Member Activities", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 180;
                var col2Width = 180;
                var col3Width = 60;
                var col4Width = 80;
                var tableWidth = col1Width + col2Width + col3Width + col4Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text with proper alignment
                var headerX = margin + 5;
                graphics.DrawString("Member", fontHeader, XBrushes.Black, headerX, tableY + 8, formatLeft);
                graphics.DrawString("Email", fontHeader, XBrushes.Black, headerX + col1Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Books", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Fines Paid", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + col3Width + 5, tableY + 8, formatLeft);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                
                foreach (var activity in data.MemberActivities.Take(50))
                {
                    // Alternate row colors
                    if (rowCount % 2 == 0)
                    {
                        graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                    }
                    
                    // Draw row border
                    graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                    
                    // Row text with proper alignment
                    graphics.DrawString(activity.MemberName, fontNormal, XBrushes.Black, headerX, y + 8, formatLeft);
                    graphics.DrawString(activity.Email, fontNormal, XBrushes.Black, headerX + col1Width + 5, y + 8, formatLeft);
                    graphics.DrawString(activity.BooksBorrowed.ToString(), fontNormal, XBrushes.Black, headerX + col1Width + col2Width + 5, y + 8, formatLeft);
                    graphics.DrawString($"₹{activity.TotalFinesPaid:F2}", fontNormal, XBrushes.Green, headerX + col1Width + col2Width + col3Width + 5, y + 8, formatLeft);
                    
                    y += rowHeight;
                    rowCount++;
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10, formatLeft);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating member activity PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateMemberActivityReportExcelAsync(DateTime startDate, DateTime endDate)
        {
            var data = await GetMemberActivityReportDataAsync(startDate, endDate);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Member Activity");

            worksheet.Cell("A1").Value = "LibraryPro - Member Activity Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A2").Value = $"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}";

            worksheet.Cell("A4").Value = "Summary";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A5").Value = "Active Members:";
            worksheet.Cell("B5").Value = data.ActiveMembers;

            worksheet.Cell("A7").Value = "Member Activities";
            worksheet.Cell("A7").Style.Font.Bold = true;

            worksheet.Cell("A8").Value = "Member";
            worksheet.Cell("B8").Value = "Email";
            worksheet.Cell("C8").Value = "Books Borrowed";
            worksheet.Cell("D8").Value = "Fines Paid";
            worksheet.Range("A8:D8").Style.Font.Bold = true;
            worksheet.Range("A8:D8").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 9;
            foreach (var activity in data.MemberActivities)
            {
                worksheet.Cell(row, 1).Value = activity.MemberName;
                worksheet.Cell(row, 2).Value = activity.Email;
                worksheet.Cell(row, 3).Value = activity.BooksBorrowed;
                worksheet.Cell(row, 4).Value = activity.TotalFinesPaid;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Overdue Books Report
        public async Task<OverdueBooksReportViewModel> GetOverdueBooksReportDataAsync()
        {
            var allLoans = await _loanRepository.GetAllLoansAsync();
            var books = await _bookRepository.GetAllAsync();
            var members = await _memberRepository.GetAllAsync();

            var overdueBooks = allLoans
                .Where(l => !l.IsReturned && l.DueDate < DateTime.UtcNow)
                .Select(loan => new OverdueBook
                {
                    BookId = loan.BookId,
                    BookTitle = books.FirstOrDefault(b => b.Id == loan.BookId)?.Title ?? "Unknown",
                    BookAuthor = books.FirstOrDefault(b => b.Id == loan.BookId)?.Author ?? "Unknown",
                    MemberId = loan.MemberId,
                    MemberName = members.FirstOrDefault(m => m.Id == loan.MemberId)?.Name ?? "Unknown",
                    MemberEmail = members.FirstOrDefault(m => m.Id == loan.MemberId)?.Email ?? "Unknown",
                    DueDate = loan.DueDate,
                    DaysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays,
                    LateFee = loan.CalculateLateFee
                })
                .OrderByDescending(b => b.DaysOverdue)
                .ToList();

            return new OverdueBooksReportViewModel
            {
                GeneratedAt = DateTime.UtcNow,
                TotalOverdueBooks = overdueBooks.Count,
                OverdueBooks = overdueBooks
            };
        }

        public async Task<byte[]> GenerateOverdueBooksReportPdfAsync()
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for overdue books report");
                var data = await GetOverdueBooksReportDataAsync();
                _logger.LogInformation("Retrieved overdue books data: {TotalBooks} books", data.TotalOverdueBooks);

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Overdue Books Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Overdue Books Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Summary section
                int y = margin + 100;
                var summaryBoxHeight = 80;
                
                // Draw summary box
                graphics.DrawRectangle(XBrushes.White, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                graphics.DrawRectangle(colorBorderDark, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                
                // Summary title
                graphics.DrawString("Summary", fontSubtitle, colorPrimary, margin + 10, y + 10);
                
                // Summary data
                var summaryY = y + 40;
                graphics.DrawString($"Total Overdue:", fontHeader, colorSecondary, margin + 10, summaryY);
                graphics.DrawString(data.TotalOverdueBooks.ToString(), fontHeader, XBrushes.Red, margin + 10 + 150, summaryY);

                y += summaryBoxHeight + 20;
                
                // Table section
                graphics.DrawString("Overdue Books", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 200;
                var col2Width = 150;
                var col3Width = 60;
                var col4Width = 60;
                var tableWidth = col1Width + col2Width + col3Width + col4Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text
                var headerX = margin + 5;
                graphics.DrawString("Book", fontHeader, XBrushes.Black, headerX, tableY + 8);
                graphics.DrawString("Member", fontHeader, XBrushes.Black, headerX + col1Width, tableY + 8);
                graphics.DrawString("Days", fontHeader, XBrushes.Black, headerX + col1Width + col2Width, tableY + 8);
                graphics.DrawString("Fee", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + col3Width, tableY + 8);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                
                foreach (var overdue in data.OverdueBooks.Take(50))
                {
                    // Alternate row colors
                    if (rowCount % 2 == 0)
                    {
                        graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                    }
                    
                    // Draw row border
                    graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                    
                    // Row text
                    graphics.DrawString(overdue.BookTitle, fontNormal, XBrushes.Black, headerX, y + 8);
                    graphics.DrawString(overdue.MemberName, fontNormal, XBrushes.Black, headerX + col1Width, y + 8);
                    graphics.DrawString(overdue.DaysOverdue.ToString(), fontNormal, XBrushes.Red, headerX + col1Width + col2Width, y + 8);
                    graphics.DrawString($"₹{overdue.LateFee:F2}", fontNormal, XBrushes.Red, headerX + col1Width + col2Width + col3Width, y + 8);
                    
                    y += rowHeight;
                    rowCount++;
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating overdue books PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateOverdueBooksReportExcelAsync()
        {
            var data = await GetOverdueBooksReportDataAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Overdue Books");

            worksheet.Cell("A1").Value = "LibraryPro - Overdue Books Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A2").Value = $"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}";

            worksheet.Cell("A4").Value = "Summary";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A5").Value = "Total Overdue Books:";
            worksheet.Cell("B5").Value = data.TotalOverdueBooks;

            worksheet.Cell("A7").Value = "Overdue Books";
            worksheet.Cell("A7").Style.Font.Bold = true;

            worksheet.Cell("A8").Value = "Book";
            worksheet.Cell("B8").Value = "Author";
            worksheet.Cell("C8").Value = "Member";
            worksheet.Cell("D8").Value = "Due Date";
            worksheet.Cell("E8").Value = "Days Overdue";
            worksheet.Cell("F8").Value = "Late Fee";
            worksheet.Range("A8:F8").Style.Font.Bold = true;
            worksheet.Range("A8:F8").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 9;
            foreach (var overdue in data.OverdueBooks)
            {
                worksheet.Cell(row, 1).Value = overdue.BookTitle;
                worksheet.Cell(row, 2).Value = overdue.BookAuthor;
                worksheet.Cell(row, 3).Value = overdue.MemberName;
                worksheet.Cell(row, 4).Value = overdue.DueDate.ToString("dd MMM yyyy");
                worksheet.Cell(row, 5).Value = overdue.DaysOverdue;
                worksheet.Cell(row, 6).Value = overdue.LateFee;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        // Inventory Status Report
        public async Task<InventoryReportViewModel> GetInventoryReportDataAsync()
        {
            var books = await _bookRepository.GetAllAsync();
            var allLoans = await _loanRepository.GetAllLoansAsync();

            var inventoryItems = books.Select(book => new InventoryItem
            {
                BookId = book.Id,
                Title = book.Title,
                Author = book.Author,
                TotalCopies = book.TotalCopies,
                AvailableCopies = book.AvailableCopies,
                BorrowedCopies = book.TotalCopies - book.AvailableCopies,
                Genre = book.Genre
            })
            .OrderBy(i => i.Title)
            .ToList();

            return new InventoryReportViewModel
            {
                GeneratedAt = DateTime.UtcNow,
                TotalBooks = inventoryItems.Count,
                TotalCopies = inventoryItems.Sum(i => i.TotalCopies),
                AvailableCopies = inventoryItems.Sum(i => i.AvailableCopies),
                BorrowedCopies = inventoryItems.Sum(i => i.BorrowedCopies),
                InventoryItems = inventoryItems
            };
        }

        public async Task<byte[]> GenerateInventoryReportPdfAsync()
        {
            try
            {
                _logger.LogInformation("Starting PDF generation for inventory report");
                var data = await GetInventoryReportDataAsync();
                _logger.LogInformation("Retrieved inventory data: {TotalBooks} books", data.TotalBooks);

                if (data.InventoryItems == null || !data.InventoryItems.Any())
                {
                    _logger.LogWarning("No inventory items found, generating empty report");
                }

                var document = new PdfDocument();
                document.Info.Title = "LibraryPro - Inventory Status Report";
                document.Info.Author = "LibraryPro";
                
                var page = document.AddPage();
                page.Size = PdfSharpCore.PageSize.A4;
                page.Orientation = PdfSharpCore.PageOrientation.Portrait;
                
                var graphics = XGraphics.FromPdfPage(page);
                var margin = 50;
                var pageWidth = page.Width.Point;
                var pageHeight = page.Height.Point;

                // Fonts
                var fontTitle = new XFont("Arial", 24, XFontStyle.Bold);
                var fontSubtitle = new XFont("Arial", 14, XFontStyle.Bold);
                var fontHeader = new XFont("Arial", 11, XFontStyle.Bold);
                var fontNormal = new XFont("Arial", 10, XFontStyle.Regular);
                var fontSmall = new XFont("Arial", 9, XFontStyle.Regular);

                // Colors
                var colorPrimary = XBrushes.DarkBlue;
                var colorSecondary = XBrushes.Gray;
                var colorTableHeader = XBrushes.LightGray;
                var colorTableAlt = XBrushes.WhiteSmoke;
                var colorBorder = XPens.LightGray;
                var colorBorderDark = XPens.DarkGray;

                // String formats for alignment
                var formatLeft = new XStringFormat();
                formatLeft.Alignment = XStringAlignment.Near;
                formatLeft.LineAlignment = XLineAlignment.Center;
                
                var formatRight = new XStringFormat();
                formatRight.Alignment = XStringAlignment.Far;
                formatRight.LineAlignment = XLineAlignment.Center;

                // Draw header background
                graphics.DrawRectangle(XBrushes.LightBlue, margin, margin, pageWidth - 2 * margin, 80);
                
                // Title
                graphics.DrawString("LibraryPro", fontTitle, colorPrimary, margin + 10, margin + 15);
                graphics.DrawString("Inventory Status Report", fontSubtitle, XBrushes.Black, margin + 10, margin + 50);
                
                // Report info
                graphics.DrawString($"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}", fontSmall, colorSecondary, margin + 10, margin + 70);

                // Summary section
                int y = margin + 100;
                var summaryBoxHeight = 100;
                
                // Draw summary box
                graphics.DrawRectangle(XBrushes.White, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                graphics.DrawRectangle(colorBorderDark, margin, y, pageWidth - 2 * margin, summaryBoxHeight);
                
                // Summary title
                graphics.DrawString("Summary", fontSubtitle, colorPrimary, margin + 10, y + 10);
                
                // Summary data in grid
                var summaryY = y + 40;
                var colWidth = (pageWidth - 2 * margin - 20) / 4;
                
                graphics.DrawString($"Total Books:", fontHeader, colorSecondary, margin + 10, summaryY, formatLeft);
                graphics.DrawString(data.TotalBooks.ToString(), fontHeader, XBrushes.Black, margin + 10 + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Total Copies:", fontHeader, colorSecondary, margin + 10 + colWidth, summaryY, formatLeft);
                graphics.DrawString(data.TotalCopies.ToString(), fontHeader, XBrushes.Black, margin + 10 + colWidth + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Available:", fontHeader, colorSecondary, margin + 10 + colWidth * 2, summaryY, formatLeft);
                graphics.DrawString(data.AvailableCopies.ToString(), fontHeader, XBrushes.Green, margin + 10 + colWidth * 2 + colWidth - 10, summaryY, formatRight);
                
                graphics.DrawString($"Borrowed:", fontHeader, colorSecondary, margin + 10 + colWidth * 3, summaryY, formatLeft);
                graphics.DrawString(data.BorrowedCopies.ToString(), fontHeader, XBrushes.Red, margin + 10 + colWidth * 3 + colWidth - 10, summaryY, formatRight);

                y += summaryBoxHeight + 20;
                
                // Table section
                graphics.DrawString("Inventory Details", fontSubtitle, colorPrimary, margin, y);
                y += 25;

                // Table header
                var tableY = y;
                var rowHeight = 25;
                var col1Width = 220;
                var col2Width = 220;
                var col3Width = 60;
                var col4Width = 60;
                var col5Width = 60;
                var tableWidth = col1Width + col2Width + col3Width + col4Width + col5Width;

                // Draw header background
                graphics.DrawRectangle(colorTableHeader, margin, tableY, tableWidth, rowHeight);
                graphics.DrawRectangle(colorBorderDark, margin, tableY, tableWidth, rowHeight);

                // Header text with proper alignment
                var headerX = margin + 5;
                graphics.DrawString("Title", fontHeader, XBrushes.Black, headerX, tableY + 8, formatLeft);
                graphics.DrawString("Author", fontHeader, XBrushes.Black, headerX + col1Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Total", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Avail", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + col3Width + 5, tableY + 8, formatLeft);
                graphics.DrawString("Borrowed", fontHeader, XBrushes.Black, headerX + col1Width + col2Width + col3Width + col4Width + 5, tableY + 8, formatLeft);

                // Table rows
                y = tableY + rowHeight;
                int rowCount = 0;
                
                if (data.InventoryItems != null && data.InventoryItems.Any())
                {
                    foreach (var item in data.InventoryItems)
                    {
                        // Alternate row colors
                        if (rowCount % 2 == 0)
                        {
                            graphics.DrawRectangle(colorTableAlt, margin, y, tableWidth, rowHeight);
                        }
                        
                        // Draw row border
                        graphics.DrawRectangle(colorBorder, margin, y, tableWidth, rowHeight);
                        
                        // Row text with proper alignment
                        graphics.DrawString(item.Title ?? "N/A", fontNormal, XBrushes.Black, headerX, y + 8, formatLeft);
                        graphics.DrawString(item.Author ?? "N/A", fontNormal, XBrushes.Black, headerX + col1Width + 5, y + 8, formatLeft);
                        graphics.DrawString(item.TotalCopies.ToString(), fontNormal, XBrushes.Black, headerX + col1Width + col2Width + 5, y + 8, formatLeft);
                        graphics.DrawString(item.AvailableCopies.ToString(), fontNormal, XBrushes.Green, headerX + col1Width + col2Width + col3Width + 5, y + 8, formatLeft);
                        graphics.DrawString(item.BorrowedCopies.ToString(), fontNormal, XBrushes.Red, headerX + col1Width + col2Width + col3Width + col4Width + 5, y + 8, formatLeft);
                        
                        y += rowHeight;
                        rowCount++;
                    }
                }
                else
                {
                    graphics.DrawString("No inventory items available", fontNormal, colorSecondary, margin, y + 8, formatLeft);
                }

                // Footer
                var footerY = pageHeight - 30;
                graphics.DrawLine(colorBorder, margin, footerY, pageWidth - margin, footerY);
                graphics.DrawString($"LibraryPro - Confidential Document | Page 1 of 1", fontSmall, colorSecondary, margin, footerY + 10, formatLeft);

                _logger.LogInformation("PDF generation completed successfully");
                
                using var stream = new MemoryStream();
                document.Save(stream, false);
                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating inventory PDF report");
                throw new InvalidOperationException($"Failed to generate PDF: {ex.Message}", ex);
            }
        }

        public async Task<byte[]> GenerateInventoryReportExcelAsync()
        {
            var data = await GetInventoryReportDataAsync();

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Inventory");

            worksheet.Cell("A1").Value = "LibraryPro - Inventory Status Report";
            worksheet.Cell("A1").Style.Font.Bold = true;
            worksheet.Cell("A1").Style.Font.FontSize = 16;

            worksheet.Cell("A2").Value = $"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}";

            worksheet.Cell("A4").Value = "Summary";
            worksheet.Cell("A4").Style.Font.Bold = true;
            worksheet.Cell("A5").Value = "Total Books:";
            worksheet.Cell("B5").Value = data.TotalBooks;
            worksheet.Cell("A6").Value = "Total Copies:";
            worksheet.Cell("B6").Value = data.TotalCopies;
            worksheet.Cell("A7").Value = "Available:";
            worksheet.Cell("B7").Value = data.AvailableCopies;
            worksheet.Cell("A8").Value = "Borrowed:";
            worksheet.Cell("B8").Value = data.BorrowedCopies;

            worksheet.Cell("A10").Value = "Inventory Details";
            worksheet.Cell("A10").Style.Font.Bold = true;

            worksheet.Cell("A11").Value = "Title";
            worksheet.Cell("B11").Value = "Author";
            worksheet.Cell("C11").Value = "Total";
            worksheet.Cell("D11").Value = "Available";
            worksheet.Cell("E11").Value = "Borrowed";
            worksheet.Range("A11:E11").Style.Font.Bold = true;
            worksheet.Range("A11:E11").Style.Fill.BackgroundColor = XLColor.LightBlue;

            int row = 12;
            foreach (var item in data.InventoryItems)
            {
                worksheet.Cell(row, 1).Value = item.Title;
                worksheet.Cell(row, 2).Value = item.Author;
                worksheet.Cell(row, 3).Value = item.TotalCopies;
                worksheet.Cell(row, 4).Value = item.AvailableCopies;
                worksheet.Cell(row, 5).Value = item.BorrowedCopies;
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
    }
}
