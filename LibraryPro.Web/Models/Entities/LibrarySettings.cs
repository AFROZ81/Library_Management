using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities
{
    public class LibrarySettings
    {
        [Key]
        public int Id { get; set; }

        [Range(0, 1000, ErrorMessage = "Daily fine rate must be a non-negative value.")]
        [Display(Name = "Daily Fine Rate (₹)")]
        public decimal DailyFineRate { get; set; } = 10.00m;

        [Range(1, 365, ErrorMessage = "Default loan period must be between 1 and 365 days.")]
        [Display(Name = "Default Loan Period (Days)")]
        public int DefaultLoanPeriodDays { get; set; } = 14;

        [Range(1, 50, ErrorMessage = "Maximum books per member must be between 1 and 50.")]
        [Display(Name = "Max Books Allowed Per Member")]
        public int MaxBooksPerMember { get; set; } = 5;

        [Range(0, 10, ErrorMessage = "Maximum renewal attempts must be between 0 and 10.")]
        [Display(Name = "Max Renewal Attempts Allowed")]
        public int MaxRenewalAttempts { get; set; } = 2;

        [Range(0, 30, ErrorMessage = "Grace period must be between 0 and 30 days.")]
        [Display(Name = "Fine Grace Period (Days)")]
        public int GracePeriodDays { get; set; } = 0;

        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}
