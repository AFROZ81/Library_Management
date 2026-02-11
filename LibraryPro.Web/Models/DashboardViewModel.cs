using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalBooks { get; set; }
        public int TotalMembers { get; set; }
        public int ActiveLoans { get; set; }
        public decimal TotalPendingFines { get; set; }
        public double AvailabilityRate { get; set; }

        // Data for Charts
        public List<string> GenreLabels { get; set; } = new();
        public List<int> GenreCounts { get; set; } = new();
        public List<int> MonthlyLoanStats { get; set; } = new();
        public List<string> WeeklyLabels { get; set; } = new();
        public List<int> WeeklyIssueCounts { get; set; } = new();
        public List<LibraryPro.Web.Models.Entities.BookLoan> CriticalOverdueLoans { get; set; } = new();
        public List<TopBorrowerDTO> TopBorrowers { get; set; } = new List<TopBorrowerDTO>();
    }
    public class TopBorrowerDTO
    {
        public string MemberName { get; set; }
        public string MemberEmail { get; set; }
        public int LoanCount { get; set; }
    }
}