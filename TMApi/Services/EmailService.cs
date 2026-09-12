namespace TMApi.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        public EmailService(ILogger<EmailService> logger)
        {
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Simulate sending an email by logging the details
            _logger.LogInformation("Sending email to: {To}, Subject: {Subject}, Body: {Body}", to, subject, body);
            await Task.CompletedTask;
        }
    }
}
