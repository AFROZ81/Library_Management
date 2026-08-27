using LibraryPro.Web.Models.Api;
using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace LibraryPro.Web.Controllers.Api;

[ApiController]
[Route("api/[controller]")]
public class LoansApiController : ControllerBase
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly ILogger<LoansApiController> _logger;

    public LoansApiController(
        ILoanRepository loanRepository,
        IBookRepository bookRepository,
        IMemberRepository memberRepository,
        ILogger<LoansApiController> logger)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
        _memberRepository = memberRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all loans
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApiLoanDto>>> GetLoans()
    {
        try
        {
            var loans = await _loanRepository.GetAllLoansAsync();
            var loanDtos = loans.Select(l => new ApiLoanDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.Book?.Title ?? string.Empty,
                MemberId = l.MemberId,
                MemberName = l.Member?.Name ?? string.Empty,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                IsReturned = l.IsReturned,
                RenewalCount = l.RenewalCount,
                LastRenewalDate = l.LastRenewalDate,
                AmountPaid = l.AmountPaid
            });
            return Ok(loanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loans");
            return StatusCode(500, new { error = "An error occurred while retrieving loans" });
        }
    }

    /// <summary>
    /// Get a loan by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiLoanDto>> GetLoan(int id)
    {
        try
        {
            var loan = await _loanRepository.GetLoanByIdAsync(id);
            if (loan == null)
            {
                return NotFound(new { error = "Loan not found" });
            }

            var loanDto = new ApiLoanDto
            {
                Id = loan.Id,
                BookId = loan.BookId,
                BookTitle = loan.Book?.Title ?? string.Empty,
                MemberId = loan.MemberId,
                MemberName = loan.Member?.Name ?? string.Empty,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                IsReturned = loan.IsReturned,
                RenewalCount = loan.RenewalCount,
                LastRenewalDate = loan.LastRenewalDate,
                AmountPaid = loan.AmountPaid
            };
            return Ok(loanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loan with ID {LoanId}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving the loan" });
        }
    }

    /// <summary>
    /// Create a new loan
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ApiLoanDto>> CreateLoan([FromBody] CreateLoanDto createLoanDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var book = await _bookRepository.GetByIdAsync(createLoanDto.BookId);
            if (book == null)
            {
                return BadRequest(new { error = "Book not found" });
            }

            if (book.AvailableCopies <= 0)
            {
                return BadRequest(new { error = "No available copies of this book" });
            }

            var member = await _memberRepository.GetByIdAsync(createLoanDto.MemberId);
            if (member == null)
            {
                return BadRequest(new { error = "Member not found" });
            }

            var loan = new BookLoan
            {
                BookId = createLoanDto.BookId,
                MemberId = createLoanDto.MemberId,
                LoanDate = DateTime.UtcNow,
                DueDate = DateTime.UtcNow.AddDays(14),
                IsReturned = false,
                RenewalCount = 0,
                AmountPaid = 0
            };

            book.AvailableCopies--;
            await _bookRepository.UpdateAsync(book);

            await _loanRepository.CreateLoanAsync(loan);

            var loanDto = new ApiLoanDto
            {
                Id = loan.Id,
                BookId = loan.BookId,
                BookTitle = book.Title ?? string.Empty,
                MemberId = loan.MemberId,
                MemberName = member.Name ?? string.Empty,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                IsReturned = loan.IsReturned,
                RenewalCount = loan.RenewalCount,
                LastRenewalDate = loan.LastRenewalDate,
                AmountPaid = loan.AmountPaid
            };

            return CreatedAtAction(nameof(GetLoan), new { id = loan.Id }, loanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating loan");
            return StatusCode(500, new { error = "An error occurred while creating the loan" });
        }
    }

    /// <summary>
    /// Return a loan
    /// </summary>
    [HttpPost("{id}/return")]
    public async Task<ActionResult<ApiLoanDto>> ReturnLoan(int id, [FromBody] ReturnLoanDto returnLoanDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var loan = await _loanRepository.GetLoanByIdAsync(id);
            if (loan == null)
            {
                return NotFound(new { error = "Loan not found" });
            }

            if (loan.IsReturned)
            {
                return BadRequest(new { error = "Loan already returned" });
            }

            loan.ReturnDate = DateTime.UtcNow;
            loan.IsReturned = true;
            loan.AmountPaid = returnLoanDto.AmountPaid;

            var book = await _bookRepository.GetByIdAsync(loan.BookId);
            if (book != null)
            {
                book.AvailableCopies++;
                await _bookRepository.UpdateAsync(book);
            }

            await _loanRepository.UpdateLoanAsync(loan);

            var loanDto = new ApiLoanDto
            {
                Id = loan.Id,
                BookId = loan.BookId,
                BookTitle = loan.Book?.Title ?? string.Empty,
                MemberId = loan.MemberId,
                MemberName = loan.Member?.Name ?? string.Empty,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                IsReturned = loan.IsReturned,
                RenewalCount = loan.RenewalCount,
                LastRenewalDate = loan.LastRenewalDate,
                AmountPaid = loan.AmountPaid
            };

            return Ok(loanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error returning loan with ID {LoanId}", id);
            return StatusCode(500, new { error = "An error occurred while returning the loan" });
        }
    }

    /// <summary>
    /// Renew a loan
    /// </summary>
    [HttpPost("{id}/renew")]
    public async Task<ActionResult<ApiLoanDto>> RenewLoan(int id)
    {
        try
        {
            var loan = await _loanRepository.GetLoanByIdAsync(id);
            if (loan == null)
            {
                return NotFound(new { error = "Loan not found" });
            }

            if (loan.IsReturned)
            {
                return BadRequest(new { error = "Cannot renew a returned loan" });
            }

            if (loan.RenewalCount >= 3)
            {
                return BadRequest(new { error = "Maximum renewal limit reached" });
            }

            loan.DueDate = loan.DueDate.AddDays(14);
            loan.RenewalCount++;
            loan.LastRenewalDate = DateTime.UtcNow;

            await _loanRepository.UpdateLoanAsync(loan);

            var loanDto = new ApiLoanDto
            {
                Id = loan.Id,
                BookId = loan.BookId,
                BookTitle = loan.Book?.Title ?? string.Empty,
                MemberId = loan.MemberId,
                MemberName = loan.Member?.Name ?? string.Empty,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                ReturnDate = loan.ReturnDate,
                IsReturned = loan.IsReturned,
                RenewalCount = loan.RenewalCount,
                LastRenewalDate = loan.LastRenewalDate,
                AmountPaid = loan.AmountPaid
            };

            return Ok(loanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing loan with ID {LoanId}", id);
            return StatusCode(500, new { error = "An error occurred while renewing the loan" });
        }
    }

    /// <summary>
    /// Get loans by member ID
    /// </summary>
    [HttpGet("member/{memberId}")]
    public async Task<ActionResult<IEnumerable<ApiLoanDto>>> GetLoansByMember(int memberId)
    {
        try
        {
            var loans = await _loanRepository.GetLoansByMemberIdAsync(memberId);
            var loanDtos = loans.Select(l => new ApiLoanDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.Book?.Title ?? string.Empty,
                MemberId = l.MemberId,
                MemberName = l.Member?.Name ?? string.Empty,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                IsReturned = l.IsReturned,
                RenewalCount = l.RenewalCount,
                LastRenewalDate = l.LastRenewalDate,
                AmountPaid = l.AmountPaid
            });
            return Ok(loanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting loans for member {MemberId}", memberId);
            return StatusCode(500, new { error = "An error occurred while retrieving loans" });
        }
    }

    /// <summary>
    /// Get overdue loans
    /// </summary>
    [HttpGet("overdue")]
    public async Task<ActionResult<IEnumerable<ApiLoanDto>>> GetOverdueLoans()
    {
        try
        {
            var loans = await _loanRepository.GetOverdueLoansAsync();
            var loanDtos = loans.Select(l => new ApiLoanDto
            {
                Id = l.Id,
                BookId = l.BookId,
                BookTitle = l.Book?.Title ?? string.Empty,
                MemberId = l.MemberId,
                MemberName = l.Member?.Name ?? string.Empty,
                LoanDate = l.LoanDate,
                DueDate = l.DueDate,
                ReturnDate = l.ReturnDate,
                IsReturned = l.IsReturned,
                RenewalCount = l.RenewalCount,
                LastRenewalDate = l.LastRenewalDate,
                AmountPaid = l.AmountPaid
            });
            return Ok(loanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue loans");
            return StatusCode(500, new { error = "An error occurred while retrieving overdue loans" });
        }
    }
}
