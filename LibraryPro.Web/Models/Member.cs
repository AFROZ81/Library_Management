using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string? Name { get; set; }

        [Required, EmailAddress]
        public string? Email { get; set; }

        [Required, Phone]
        [Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Join Date")]
        public DateTime MembershipDate { get; set; } = DateTime.Now;

        // Email preferences
        public bool ReceiveOverdueNotices { get; set; } = true;
        public bool ReceiveDueDateReminders { get; set; } = true;
        public bool ReceiveReservationAlerts { get; set; } = true;

        // Navigation property for the loans
        public virtual ICollection<BookLoan> Loans { get; set; } = new List<BookLoan>();
    }
}