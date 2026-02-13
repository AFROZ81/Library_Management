using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Models
{
    public class FinePayment
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public int BookLoanId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Member Member { get; set; }
        public virtual BookLoan Loan { get; set; }
    }
}
