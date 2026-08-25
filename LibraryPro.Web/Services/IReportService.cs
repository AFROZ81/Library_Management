namespace LibraryPro.Web.Services
{
    public interface IReportService
    {
        // Circulation Reports
        Task<byte[]> GenerateCirculationReportPdfAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateCirculationReportExcelAsync(DateTime startDate, DateTime endDate);
        Task<CirculationReportViewModel> GetCirculationReportDataAsync(DateTime startDate, DateTime endDate);

        // Financial Reports
        Task<byte[]> GenerateFinancialReportPdfAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateFinancialReportExcelAsync(DateTime startDate, DateTime endDate);
        Task<FinancialReportViewModel> GetFinancialReportDataAsync(DateTime startDate, DateTime endDate);

        // Popular Books Reports
        Task<byte[]> GeneratePopularBooksReportPdfAsync(DateTime startDate, DateTime endDate, int topN = 20);
        Task<byte[]> GeneratePopularBooksReportExcelAsync(DateTime startDate, DateTime endDate, int topN = 20);
        Task<PopularBooksReportViewModel> GetPopularBooksReportDataAsync(DateTime startDate, DateTime endDate, int topN = 20);

        // Member Activity Reports
        Task<byte[]> GenerateMemberActivityReportPdfAsync(DateTime startDate, DateTime endDate);
        Task<byte[]> GenerateMemberActivityReportExcelAsync(DateTime startDate, DateTime endDate);
        Task<MemberActivityReportViewModel> GetMemberActivityReportDataAsync(DateTime startDate, DateTime endDate);

        // Overdue Books Report
        Task<byte[]> GenerateOverdueBooksReportPdfAsync();
        Task<byte[]> GenerateOverdueBooksReportExcelAsync();
        Task<OverdueBooksReportViewModel> GetOverdueBooksReportDataAsync();

        // Inventory Status Report
        Task<byte[]> GenerateInventoryReportPdfAsync();
        Task<byte[]> GenerateInventoryReportExcelAsync();
        Task<InventoryReportViewModel> GetInventoryReportDataAsync();
    }

    // View Models for Reports
    public class CirculationReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int TotalLoansIssued { get; set; }
        public int TotalLoansReturned { get; set; }
        public int ActiveLoans { get; set; }
        public double AverageLoanDuration { get; set; }
        public List<DailyCirculation> DailyCirculation { get; set; } = new();
    }

    public class DailyCirculation
    {
        public DateTime Date { get; set; }
        public int LoansIssued { get; set; }
        public int LoansReturned { get; set; }
    }

    public class FinancialReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal TotalFinesCollected { get; set; }
        public int TotalPayments { get; set; }
        public decimal AveragePaymentAmount { get; set; }
        public List<PaymentRecord> Payments { get; set; } = new();
    }

    public class PaymentRecord
    {
        public DateTime PaymentDate { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
    }

    public class PopularBooksReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<PopularBook> PopularBooks { get; set; } = new();
    }

    public class PopularBook
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int TimesBorrowed { get; set; }
        public List<string> Genre { get; set; } = new();
    }

    public class MemberActivityReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ActiveMembers { get; set; }
        public List<MemberActivity> MemberActivities { get; set; } = new();
    }

    public class MemberActivity
    {
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int BooksBorrowed { get; set; }
        public decimal TotalFinesPaid { get; set; }
    }

    public class OverdueBooksReportViewModel
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalOverdueBooks { get; set; }
        public List<OverdueBook> OverdueBooks { get; set; } = new();
    }

    public class OverdueBook
    {
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string BookAuthor { get; set; } = string.Empty;
        public int MemberId { get; set; }
        public string MemberName { get; set; } = string.Empty;
        public string MemberEmail { get; set; } = string.Empty;
        public DateTime DueDate { get; set; }
        public int DaysOverdue { get; set; }
        public decimal LateFee { get; set; }
    }

    public class InventoryReportViewModel
    {
        public DateTime GeneratedAt { get; set; }
        public int TotalBooks { get; set; }
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopies { get; set; }
        public List<InventoryItem> InventoryItems { get; set; } = new();
    }

    public class InventoryItem
    {
        public int BookId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public int BorrowedCopies { get; set; }
        public List<string> Genre { get; set; } = new();
    }
}
