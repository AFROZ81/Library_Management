using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace LibraryPro.Web.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            IOptions<EmailSettings> emailSettings,
            ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            await SendEmailAsync(new List<string> { toEmail }, subject, htmlBody, textBody);
        }

        public async Task SendEmailAsync(List<string> toEmails, string subject, string htmlBody, string? textBody = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.FromName, _emailSettings.FromEmail));
                
                foreach (var email in toEmails)
                {
                    message.To.Add(new MailboxAddress("", email));
                }

                message.Subject = subject;

                var builder = new BodyBuilder();
                builder.HtmlBody = htmlBody;
                if (!string.IsNullOrEmpty(textBody))
                {
                    builder.TextBody = textBody;
                }

                message.Body = builder.ToMessageBody();

                using var client = new SmtpClient();
                try
                {
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, 
                        _emailSettings.SmtpUseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);
                    
                    if (!string.IsNullOrEmpty(_emailSettings.SmtpUsername) && 
                        !string.IsNullOrEmpty(_emailSettings.SmtpPassword))
                    {
                        await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                    }

                    await client.SendAsync(message);
                    _logger.LogInformation("Email sent successfully to {RecipientCount} recipients", toEmails.Count);
                }
                finally
                {
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(", ", toEmails));
                throw;
            }
        }

        public async Task<bool> SendEmailWithLoggingAsync(string toEmail, string subject, string htmlBody, string? textBody = null)
        {
            try
            {
                await SendEmailAsync(toEmail, subject, htmlBody, textBody);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email sending failed for {Email}", toEmail);
                return false;
            }
        }
    }

    public class EmailSettings
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; } = 587;
        public bool SmtpUseSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
    }
}
