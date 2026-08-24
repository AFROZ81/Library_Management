namespace LibraryPro.Web.Services
{
    public interface IEmailTemplateService
    {
        string GenerateOverdueNoticeEmail(string memberName, string bookTitle, DateTime dueDate, decimal fineAmount);
        string GenerateDueDateReminderEmail(string memberName, string bookTitle, DateTime dueDate, int daysUntilDue);
        string GenerateWelcomeEmail(string memberName, string memberEmail);
        string GenerateReservationAvailableEmail(string memberName, string bookTitle);
    }
}
