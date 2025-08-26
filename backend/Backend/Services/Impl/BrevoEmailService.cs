using MailKit.Net.Smtp;
using MimeKit;

namespace Backend.Services.Impl
{
    public interface ISmtpClient : IDisposable
    {
        Task ConnectAsync(string host, int port, MailKit.Security.SecureSocketOptions options);
        Task AuthenticateAsync(string user, string password);
        Task SendAsync(MimeMessage message);
        Task DisconnectAsync(bool quit);
    }

    public class SmtpClientAdapter : ISmtpClient
    {
        private readonly SmtpClient _client = new();
        public Task ConnectAsync(string host, int port, MailKit.Security.SecureSocketOptions options) => _client.ConnectAsync(host, port, options);
        public Task AuthenticateAsync(string user, string password) => _client.AuthenticateAsync(user, password);
        public Task SendAsync(MimeMessage message) => _client.SendAsync(message);
        public Task DisconnectAsync(bool quit) => _client.DisconnectAsync(quit);
        public void Dispose() => _client.Dispose();
    }

    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<BrevoEmailService> _logger;
        private readonly ISmtpClient _smtpClient;

        public BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger, ISmtpClient smtpClient)
        {
            _config = config;
            _logger = logger;
            _smtpClient = smtpClient;
        }

        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="resetCode">The password reset link to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(
                _config["Email:FromName"],
                _config["Email:FromEmail"]));
            emailMessage.To.Add(MailboxAddress.Parse(toEmail));
            emailMessage.Subject = "Reset your password";
            var resetLink = _config["Email:ResetLink"];
            emailMessage.Body = new TextPart("html") { Text = $"<p>Click <a href=\"{resetLink}/?code={resetCode}\">here</a> to reset your password.</p>" };

            int port;
            port = int.TryParse(_config["Email:Port"], out port) ? port : 587;
            await _smtpClient.ConnectAsync(_config["Email:MailServer"]!, port, MailKit.Security.SecureSocketOptions.StartTls);
            await _smtpClient.AuthenticateAsync(_config["Email:SMTPUser"]!, _config["Email:SMTPPassword"]!);
            await _smtpClient.SendAsync(emailMessage);
            await _smtpClient.DisconnectAsync(true);

            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
    }
}