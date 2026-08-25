using ClosedXML.Excel;
using LibraryPro.Web.Data;
using LibraryPro.Web.Repositories;
using LibraryPro.Web.Models;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace LibraryPro.Web.Services
{
    public class ReportService : IReportService
    {
        static ReportService()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

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

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(container =>
                        {
                            container.Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                    column.Item().Text("Circulation Report").FontSize(14);
                                    column.Item().Text($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                });
                            });
                        });

                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);

                            // Summary Section
                            container.Column(column =>
                            {
                                column.Item().Element(element =>
                                {
                                    element.Border(1).Padding(10).Background(Colors.Grey.Lighten3);
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Total Loans Issued:").Bold();
                                        row.ConstantItem(100).Text(data.TotalLoansIssued.ToString());
                                    });
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Total Loans Returned:").Bold();
                                        row.ConstantItem(100).Text(data.TotalLoansReturned.ToString());
                                    });
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Active Loans:").Bold();
                                        row.ConstantItem(100).Text(data.ActiveLoans.ToString());
                                    });
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Avg Duration (days):").Bold();
                                        row.ConstantItem(100).Text($"{data.AverageLoanDuration:F1}");
                                    });
                                });

                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                                // Daily Circulation Table
                                column.Item().Text("Daily Circulation").Bold().FontSize(12);
                                column.Item().PaddingTop(10);

                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(100);
                                        columns.ConstantColumn(100);
                                        columns.ConstantColumn(100);
                                    });

                                    table.Header(header =>
                                    {
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Date").Bold();
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Issued").Bold();
                                        header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Returned").Bold();
                                    });

                                    foreach (var daily in data.DailyCirculation)
                                    {
                                        table.Cell().Element(cell => cell.Padding(5)).Text(daily.Date.ToString("dd MMM yyyy"));
                                        table.Cell().Element(cell => cell.Padding(5)).Text(daily.LoansIssued.ToString());
                                        table.Cell().Element(cell => cell.Padding(5)).Text(daily.LoansReturned.ToString());
                                    }
                                });
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                    });
                });

                _logger.LogInformation("PDF document created successfully, generating PDF bytes");
                var pdfBytes = document.GeneratePdf();
                _logger.LogInformation("PDF generation completed successfully, size: {Size} bytes", pdfBytes.Length);
                return pdfBytes;
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
            var data = await GetFinancialReportDataAsync(startDate, endDate);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Financial Report").FontSize(14);
                                column.Item().Text($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Content().Element(container =>
                    {
                        container.PaddingVertical(1, Unit.Centimetre);

                        container.Column(column =>
                        {
                            column.Item().Element(element =>
                            {
                                element.Border(1).Padding(10).Background(Colors.Grey.Lighten3);
                                element.Row(row =>
                                {
                                    row.ConstantItem(200).Text("Total Fines Collected:").Bold();
                                    row.ConstantItem(100).Text($"₹{data.TotalFinesCollected:F2}");
                                });
                                element.Row(row =>
                                {
                                    row.ConstantItem(200).Text("Total Payments:").Bold();
                                    row.ConstantItem(100).Text(data.TotalPayments.ToString());
                                });
                                element.Row(row =>
                                {
                                    row.ConstantItem(200).Text("Average Payment:").Bold();
                                    row.ConstantItem(100).Text($"₹{data.AveragePaymentAmount:F2}");
                                });
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            column.Item().Text("Payment Records").Bold().FontSize(12);
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(100);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Date").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Member").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Amount").Bold();
                                });

                                foreach (var payment in data.Payments.Take(50))
                                {
                                    table.Cell().Element(cell => cell.Padding(5)).Text(payment.PaymentDate.ToString("dd MMM yyyy"));
                                    table.Cell().Element(cell => cell.Padding(5)).Text(payment.MemberName);
                                    table.Cell().Element(cell => cell.Padding(5)).Text($"₹{payment.Amount:F2}");
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
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
            var data = await GetPopularBooksReportDataAsync(startDate, endDate, topN);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Popular Books Report").FontSize(14);
                                column.Item().Text($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Content().Element(container =>
                    {
                        container.PaddingVertical(1, Unit.Centimetre);

                        container.Column(column =>
                        {
                            column.Item().Text($"Top {data.PopularBooks.Count} Most Borrowed Books").Bold().FontSize(12);
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(40);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("#").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Title").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Author").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Borrowed").Bold();
                                });

                                int rank = 1;
                                foreach (var book in data.PopularBooks)
                                {
                                    table.Cell().Element(cell => cell.Padding(5)).Text(rank.ToString());
                                    table.Cell().Element(cell => cell.Padding(5)).Text(book.Title);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(book.Author);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(book.TimesBorrowed.ToString());
                                    rank++;
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
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
            var data = await GetMemberActivityReportDataAsync(startDate, endDate);

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Member Activity Report").FontSize(14);
                                column.Item().Text($"Period: {startDate:dd MMM yyyy} - {endDate:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Content().Element(container =>
                    {
                        container.PaddingVertical(1, Unit.Centimetre);

                        container.Column(column =>
                        {
                            column.Item().Element(element =>
                            {
                                element.Border(1).Padding(10).Background(Colors.Grey.Lighten3);
                                element.Row(row =>
                                {
                                    row.ConstantItem(150).Text("Active Members:").Bold();
                                    row.ConstantItem(100).Text(data.ActiveMembers.ToString());
                                });
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            column.Item().Text("Member Activities").Bold().FontSize(12);
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(80);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Member").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Email").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Books").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Fines Paid").Bold();
                                });

                                foreach (var activity in data.MemberActivities.Take(50))
                                {
                                    table.Cell().Element(cell => cell.Padding(5)).Text(activity.MemberName);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(activity.Email);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(activity.BooksBorrowed.ToString());
                                    table.Cell().Element(cell => cell.Padding(5)).Text($"₹{activity.TotalFinesPaid:F2}");
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
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
            var data = await GetOverdueBooksReportDataAsync();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(container =>
                    {
                        container.Row(row =>
                        {
                            row.RelativeItem().Column(column =>
                            {
                                column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                column.Item().Text("Overdue Books Report").FontSize(14);
                                column.Item().Text($"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                            });
                        });
                    });

                    page.Content().Element(container =>
                    {
                        container.PaddingVertical(1, Unit.Centimetre);

                        container.Column(column =>
                        {
                            column.Item().Element(element =>
                            {
                                element.Border(1).Padding(10).Background(Colors.Red.Lighten3);
                                element.Row(row =>
                                {
                                    row.ConstantItem(150).Text("Total Overdue:").Bold();
                                    row.ConstantItem(100).Text(data.TotalOverdueBooks.ToString()).FontColor(Colors.Red.Darken2);
                                });
                            });

                            column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                            column.Item().Text("Overdue Books").Bold().FontSize(12);
                            column.Item().PaddingTop(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(60);
                                    columns.ConstantColumn(60);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Book").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Member").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Days").Bold();
                                    header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Fee").Bold();
                                });

                                foreach (var overdue in data.OverdueBooks.Take(50))
                                {
                                    table.Cell().Element(cell => cell.Padding(5)).Text(overdue.BookTitle);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(overdue.MemberName);
                                    table.Cell().Element(cell => cell.Padding(5)).Text(overdue.DaysOverdue.ToString());
                                    table.Cell().Element(cell => cell.Padding(5)).Text($"₹{overdue.LateFee:F2}");
                                }
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
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

                var document = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10));

                        page.Header().Element(container =>
                        {
                            container.Row(row =>
                            {
                                row.RelativeItem().Column(column =>
                                {
                                    column.Item().Text("LibraryPro").Bold().FontSize(20).FontColor(Colors.Blue.Darken2);
                                    column.Item().Text("Inventory Status Report").FontSize(14);
                                    column.Item().Text($"Generated: {data.GeneratedAt:dd MMM yyyy HH:mm}").FontSize(10).FontColor(Colors.Grey.Darken1);
                                });
                            });
                        });

                        page.Content().Element(container =>
                        {
                            container.PaddingVertical(1, Unit.Centimetre);

                            container.Column(column =>
                            {
                                column.Item().Element(element =>
                                {
                                    element.Border(1).Padding(10).Background(Colors.Grey.Lighten3);
                                });
                                
                                column.Item().Element(element =>
                                {
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Total Books:").Bold();
                                        row.ConstantItem(100).Text(data.TotalBooks.ToString());
                                    });
                                });
                                
                                column.Item().Element(element =>
                                {
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Total Copies:").Bold();
                                        row.ConstantItem(100).Text(data.TotalCopies.ToString());
                                    });
                                });
                                
                                column.Item().Element(element =>
                                {
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Available:").Bold();
                                        row.ConstantItem(100).Text(data.AvailableCopies.ToString());
                                    });
                                });
                                
                                column.Item().Element(element =>
                                {
                                    element.Row(row =>
                                    {
                                        row.ConstantItem(150).Text("Borrowed:").Bold();
                                        row.ConstantItem(100).Text(data.BorrowedCopies.ToString());
                                    });
                                });

                                column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);

                                column.Item().Text("Inventory Details").Bold().FontSize(12);
                                column.Item().PaddingTop(10);

                                if (data.InventoryItems != null && data.InventoryItems.Any())
                                {
                                    column.Item().Table(table =>
                                    {
                                        table.ColumnsDefinition(columns =>
                                        {
                                            columns.RelativeColumn();
                                            columns.RelativeColumn();
                                            columns.ConstantColumn(60);
                                            columns.ConstantColumn(60);
                                            columns.ConstantColumn(60);
                                        });

                                        table.Header(header =>
                                        {
                                            header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Title").Bold();
                                            header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Author").Bold();
                                            header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Total").Bold();
                                            header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Avail").Bold();
                                            header.Cell().Element(cell => cell.Background(Colors.Blue.Lighten4).Padding(5)).Text("Borrowed").Bold();
                                        });

                                        foreach (var item in data.InventoryItems)
                                        {
                                            table.Cell().Element(cell => cell.Padding(5)).Text(item.Title ?? "N/A");
                                            table.Cell().Element(cell => cell.Padding(5)).Text(item.Author ?? "N/A");
                                            table.Cell().Element(cell => cell.Padding(5)).Text(item.TotalCopies.ToString());
                                            table.Cell().Element(cell => cell.Padding(5)).Text(item.AvailableCopies.ToString());
                                            table.Cell().Element(cell => cell.Padding(5)).Text(item.BorrowedCopies.ToString());
                                        }
                                    });
                                }
                                else
                                {
                                    column.Item().Text("No inventory items available").FontColor(Colors.Grey.Darken1);
                                }
                            });
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                        });
                    });
                });

                _logger.LogInformation("PDF document created successfully, generating PDF bytes");
                var pdfBytes = document.GeneratePdf();
                _logger.LogInformation("PDF generation completed successfully, size: {Size} bytes", pdfBytes.Length);
                return pdfBytes;
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
