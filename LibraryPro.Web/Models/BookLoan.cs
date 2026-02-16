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
        public Book? Book { get; set; }

        [Required]
        public int MemberId { get; set; }
        [ForeignKey("MemberId")]
        public Member? Member { get; set; }

        public DateTime LoanDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14); // Default 2-week loan
        public DateTime? ReturnDate { get; set; } // Null until book is returned

        public decimal AmountPaid { get; set; } = 0;

        [NotMapped] // This tells EF Core NOT to create a column in SSMS for this
        public decimal CalculateLateFee
        {
            get
            {
                if (IsReturned || DueDate >= DateTime.Now.Date) return 0;

                int daysOverdue = (DateTime.Now.Date - DueDate.Date).Days;
                decimal totalFee = daysOverdue * 10; // e.g., 10 rupees per day

                // Subtract what they already paid to get the "Current Fine"
                return Math.Max(0, totalFee - AmountPaid);
            }
        }
        public bool IsReturned { get; set; } = false;
    }
}