using LibraryPro.Web.Models.Entities;

namespace LibraryPro.Web.Models
{
    public class MemberProfileViewModel
    {
        public int MemberId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public decimal TotalUnpaidFine { get; set; }
        public List<BookLoan> ActiveLoans { get; set; } = new();
        public List<BookLoan> PastHistory { get; set; } = new();
    }
}