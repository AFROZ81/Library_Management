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
        public Book? Book { get; set; }

        [Required]
        public int MemberId { get; set; }
        public Member? Member { get; set; }

        public DateTime LoanDate { get; set; } = DateTime.Now;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14);
        public DateTime? ReturnDate { get; set; }

        public decimal AmountPaid { get; set; } = 0;

        public int RenewalCount { get; set; } = 0;
        public DateTime? LastRenewalDate { get; set; }

        [NotMapped]
        public decimal CalculateLateFee
        {
            get
            {
                return GetLateFeeWithRate(10.00m, 0);
            }
        }

        public decimal GetLateFeeWithRate(decimal dailyRate, int gracePeriodDays = 0)
        {
            if (IsReturned || DueDate.AddDays(gracePeriodDays) >= DateTime.Now.Date) return 0;

            int daysOverdue = (DateTime.Now.Date - DueDate.Date).Days;
            if (daysOverdue <= gracePeriodDays) return 0;

            decimal totalFee = (daysOverdue - gracePeriodDays) * dailyRate;
            return Math.Max(0, totalFee - AmountPaid);
        }

        public bool IsReturned { get; set; } = false;
    }
}
