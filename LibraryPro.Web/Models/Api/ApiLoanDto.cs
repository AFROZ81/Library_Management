namespace LibraryPro.Web.Models.Api;

public class ApiLoanDto
{
    public int Id { get; set; }
    public int BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public bool IsReturned { get; set; }
    public int RenewalCount { get; set; }
    public DateTime? LastRenewalDate { get; set; }
    public decimal AmountPaid { get; set; }
}

public class CreateLoanDto
{
    public int BookId { get; set; }
    public int MemberId { get; set; }
}

public class ReturnLoanDto
{
    public decimal AmountPaid { get; set; }
}

public class RenewLoanDto
{
    // No additional fields needed for renewal
}
