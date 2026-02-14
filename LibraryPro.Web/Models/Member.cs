using System.ComponentModel.DataAnnotations;

namespace LibraryPro.Web.Models.Entities
{
    public class Member
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, Phone]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [Display(Name = "Join Date")]
        public DateTime MembershipDate { get; set; } = DateTime.Now;

        // Navigation property for the loans we will create later
        public ICollection<Book>? BorrowedBooks { get; set; }
        public virtual ICollection<BookLoan> Loans { get; set; } = new List<BookLoan>();
    }
}