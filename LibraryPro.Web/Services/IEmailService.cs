namespace LibraryPro.Web.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
        Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody, string? textBody = null);
        Task<bool> SendEmailWithLoggingAsync(string toEmail, string subject, string htmlBody, string? textBody = null);
    }
}
