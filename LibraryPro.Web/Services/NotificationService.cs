using LibraryPro.Web.Models.Entities;
using LibraryPro.Web.Repositories;

namespace LibraryPro.Web.Services
{
    public interface INotificationService
    {
        Task SendOverdueNoticesAsync();
        Task SendDueDateRemindersAsync();
        Task SendWelcomeEmailAsync(Member member);
        Task SendReservationAvailableEmailAsync(Member member, Book book);
    }

    public class NotificationService : INotificationService
    {
        private readonly EmailQueue _emailQueue;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly ILoanRepository _loanRepository;
        private readonly IMemberRepository _memberRepository;
        private readonly IBookRepository _bookRepository;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            EmailQueue emailQueue,
            IEmailTemplateService emailTemplateService,
            ILoanRepository loanRepository,
            IMemberRepository memberRepository,
            IBookRepository bookRepository,
            ILogger<NotificationService> logger)
        {
            _emailQueue = emailQueue;
            _emailTemplateService = emailTemplateService;
            _loanRepository = loanRepository;
            _memberRepository = memberRepository;
            _bookRepository = bookRepository;
            _logger = logger;
        }

        public async Task SendOverdueNoticesAsync()
        {
            try
            {
                _logger.LogInformation("Starting overdue notice check...");

                var overdueLoans = await _loanRepository.GetOverdueLoansAsync();
                
                foreach (var loan in overdueLoans)
                {
                    var member = await _memberRepository.GetByIdAsync(loan.MemberId);
                    var book = await _bookRepository.GetByIdAsync(loan.BookId);

                    if (member != null && book != null && member.ReceiveOverdueNotices)
                    {
                        var fineAmount = loan.CalculateLateFee;
                        var emailBody = _emailTemplateService.GenerateOverdueNoticeEmail(
                            member.Name ?? "Member",
                            book.Title ?? "Unknown Book",
                            loan.DueDate,
                            fineAmount);

                        var emailMessage = new EmailMessage
                        {
                            ToEmail = member.Email ?? string.Empty,
                            Subject = "Overdue Book Notice - LibraryPro",
                            HtmlBody = emailBody,
                            EmailType = "OverdueNotice",
                            Metadata = new Dictionary<string, object>
                            {
                                { "LoanId", loan.Id },
                                { "BookId", book.Id },
                                { "MemberId", member.Id }
                            }
                        };

                        _emailQueue.Enqueue(emailMessage);
                        _logger.LogInformation("Queued overdue notice for {Email} - Book: {Book}", member.Email, book.Title);
                    }
                }

                _logger.LogInformation("Overdue notice check completed. {Count} notices queued.", overdueLoans.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending overdue notices");
                throw;
            }
        }

        public async Task SendDueDateRemindersAsync()
        {
            try
            {
                _logger.LogInformation("Starting due date reminder check...");

                // Get loans due in 3 days
                var loansDueIn3Days = await _loanRepository.GetLoansDueInDaysAsync(3);
                // Get loans due in 1 day
                var loansDueIn1Day = await _loanRepository.GetLoansDueInDaysAsync(1);

                var allReminderLoans = loansDueIn3Days.Concat(loansDueIn1Day).DistinctBy(l => l.Id);

                foreach (var loan in allReminderLoans)
                {
                    var member = await _memberRepository.GetByIdAsync(loan.MemberId);
                    var book = await _bookRepository.GetByIdAsync(loan.BookId);

                    if (member != null && book != null && member.ReceiveDueDateReminders)
                    {
                        var daysUntilDue = (loan.DueDate - DateTime.Now).Days;
                        
                        // Only send if it's exactly 3 days or 1 day before
                        if (daysUntilDue == 3 || daysUntilDue == 1)
                        {
                            var emailBody = _emailTemplateService.GenerateDueDateReminderEmail(
                                member.Name ?? "Member",
                                book.Title ?? "Unknown Book",
                                loan.DueDate,
                                daysUntilDue);

                            var emailMessage = new EmailMessage
                            {
                                ToEmail = member.Email ?? string.Empty,
                                Subject = $"Due Date Reminder - {daysUntilDue} Day{(daysUntilDue > 1 ? "s" : "")} Remaining",
                                HtmlBody = emailBody,
                                EmailType = "DueDateReminder",
                                Metadata = new Dictionary<string, object>
                                {
                                    { "LoanId", loan.Id },
                                    { "BookId", book.Id },
                                    { "MemberId", member.Id },
                                    { "DaysUntilDue", daysUntilDue }
                                }
                            };

                            _emailQueue.Enqueue(emailMessage);
                            _logger.LogInformation("Queued due date reminder for {Email} - Book: {Book} - Days: {Days}", 
                                member.Email, book.Title, daysUntilDue);
                        }
                    }
                }

                _logger.LogInformation("Due date reminder check completed. {Count} reminders queued.", allReminderLoans.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending due date reminders");
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(Member member)
        {
            try
            {
                var emailBody = _emailTemplateService.GenerateWelcomeEmail(
                    member.Name ?? "Member",
                    member.Email ?? string.Empty);

                var emailMessage = new EmailMessage
                {
                    ToEmail = member.Email ?? string.Empty,
                    Subject = "Welcome to LibraryPro!",
                    HtmlBody = emailBody,
                    EmailType = "Welcome",
                    Metadata = new Dictionary<string, object>
                    {
                        { "MemberId", member.Id }
                    }
                };

                _emailQueue.Enqueue(emailMessage);
                _logger.LogInformation("Queued welcome email for {Email}", member.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", member.Email);
                throw;
            }
        }

        public async Task SendReservationAvailableEmailAsync(Member member, Book book)
        {
            try
            {
                if (member.ReceiveReservationAlerts)
                {
                    var emailBody = _emailTemplateService.GenerateReservationAvailableEmail(
                        member.Name ?? "Member",
                        book.Title ?? "Unknown Book");

                    var emailMessage = new EmailMessage
                    {
                        ToEmail = member.Email ?? string.Empty,
                        Subject = "Reserved Book Available - LibraryPro",
                        HtmlBody = emailBody,
                        EmailType = "ReservationAvailable",
                        Metadata = new Dictionary<string, object>
                        {
                            { "MemberId", member.Id },
                            { "BookId", book.Id }
                        }
                    };

                    _emailQueue.Enqueue(emailMessage);
                    _logger.LogInformation("Queued reservation available email for {Email} - Book: {Book}", 
                        member.Email, book.Title);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending reservation available email to {Email}", member.Email);
                throw;
            }
        }
    }
}
