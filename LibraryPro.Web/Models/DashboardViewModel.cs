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
    }
}