using LibraryPro.Web.Repositories;

namespace LibraryPro.Web.Services
{
    public class EmailBackgroundService : BackgroundService
    {
        private readonly EmailQueue _emailQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailBackgroundService> _logger;
        private readonly TimeSpan _retryDelay = TimeSpan.FromMinutes(5);

        public EmailBackgroundService(
            EmailQueue emailQueue,
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EmailBackgroundService> logger)
        {
            _emailQueue = emailQueue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var emailMessage = await _emailQueue.DequeueAsync(stoppingToken);
                    
                    if (emailMessage != null)
                    {
                        await ProcessEmailAsync(emailMessage);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Shutdown requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email from queue");
                    await Task.Delay(_retryDelay, stoppingToken);
                }
            }

            _logger.LogInformation("Email Background Service stopped.");
        }

        private async Task ProcessEmailAsync(EmailMessage emailMessage)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var emailLogRepository = scope.ServiceProvider.GetRequiredService<IEmailLogRepository>();

            var emailLog = new Models.Entities.EmailLog
            {
                ToEmail = emailMessage.ToEmail,
                Subject = emailMessage.Subject,
                Body = emailMessage.HtmlBody,
                SentAt = DateTime.UtcNow,
                EmailType = emailMessage.EmailType,
                Status = "Processing",
                IsSuccess = false
            };

            try
            {
                _logger.LogInformation("Sending email to {Email} - Type: {Type}", emailMessage.ToEmail, emailMessage.EmailType);
                
                var success = await emailService.SendEmailWithLoggingAsync(
                    emailMessage.ToEmail,
                    emailMessage.Subject,
                    emailMessage.HtmlBody,
                    emailMessage.TextBody);

                emailLog.IsSuccess = success;
                emailLog.Status = success ? "Sent" : "Failed";
                
                if (!success)
                {
                    emailLog.ErrorMessage = "Email sending failed (unknown error)";
                }

                _logger.LogInformation("Email to {Email} - Status: {Status}", emailMessage.ToEmail, emailLog.Status);
            }
            catch (Exception ex)
            {
                emailLog.IsSuccess = false;
                emailLog.Status = "Failed";
                emailLog.ErrorMessage = ex.Message;
                _logger.LogError(ex, "Failed to send email to {Email}", emailMessage.ToEmail);
            }
            finally
            {
                await emailLogRepository.AddAsync(emailLog);
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Email Background Service is stopping. Processing remaining emails...");
            
            // Process remaining emails before stopping
            while (_emailQueue.Count > 0)
            {
                var emailMessage = await _emailQueue.DequeueAsync(cancellationToken);
                if (emailMessage != null)
                {
                    await ProcessEmailAsync(emailMessage);
                }
            }

            await base.StopAsync(cancellationToken);
        }
    }
}
