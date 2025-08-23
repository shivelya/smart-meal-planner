using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services.Impl
{
    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="resetLink">The password reset link to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(
                _config["Email:FromName"],
                _config["Email:FromEmail"]));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = "Reset your password";
            emailMessage.Body = new TextPart("html") { Text = $"<p>Click <a href=\"{resetLink}\">here</a> to reset your password.</p>" };

            using var client = new SmtpClient();
            await client.ConnectAsync("smtp-relay.brevo.com", 587, MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_config["Email:SMTPUser"], _config["Email:SMTPPassword"]);
            await client.SendAsync(emailMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
    }
}