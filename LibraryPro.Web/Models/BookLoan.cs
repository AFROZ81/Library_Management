using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryPro.Web.Models.Entities
{
    public class BookLoan
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        [ForeignKey("BookId")]
        public Book Book { get; set; }

        [Required]
        public int MemberId { get; set; }
        [ForeignKey("MemberId")]
        public Member Member { get; set; }

        public DateTime LoanDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14); // Default 2-week loan
        public DateTime? ReturnDate { get; set; } // Null until book is returned

        [NotMapped] // This tells EF Core NOT to create a column in SSMS for this
        public decimal CalculateLateFee
        {
            get
            {
                var calculationDate = IsReturned ? (ReturnDate ?? DateTime.Now) : DateTime.Now;
                if (calculationDate > DueDate)
                {
                    var overdueDays = (calculationDate - DueDate).Days;
                    return overdueDays * 2.00m; // Charging $2.00 per day
                }
                return 0.00m;
            }
        }

        public bool IsReturned { get; set; } = false;
    }
}