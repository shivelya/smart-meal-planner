using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Backend.Services.Impl
{
    public interface ISmtpClient : IDisposable
    {
        Task ConnectAsync(string host, int port, SecureSocketOptions options);
        Task AuthenticateAsync(string user, string password);
        Task SendAsync(MimeMessage message);
        Task DisconnectAsync(bool quit);
    }

    public class SmtpClientAdapter : ISmtpClient
    {
        public SmtpClientAdapter() { _client = new(); }

        public SmtpClientAdapter(SmtpClient client) { _client = client; }

        private readonly SmtpClient _client;
        public Task ConnectAsync(string host, int port, SecureSocketOptions options) => _client.ConnectAsync(host, port, options);
        public Task AuthenticateAsync(string user, string password) => _client.AuthenticateAsync(user, password);
        public Task SendAsync(MimeMessage message) => _client.SendAsync(message);
        public Task DisconnectAsync(bool quit) => _client.DisconnectAsync(quit);
        public void Dispose() => _client.Dispose();
    }

    public class BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger, ISmtpClient smtpClient) : IEmailService
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<BrevoEmailService> _logger = logger;
        private readonly ISmtpClient _smtpClient = smtpClient;

        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="resetCode">The password reset link to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPasswordResetEmailAsync(string toEmail, string resetCode)
        {
            _logger.LogInformation("Entering SendPasswordResetEmailAsync: toEmail={ToEmail}, resetCode={ResetCode}", toEmail, resetCode);
            try
            {
                if (string.IsNullOrWhiteSpace(toEmail))
                {
                    _logger.LogWarning("SendPasswordResetEmailAsync: toEmail is null or empty");
                    throw new ArgumentException("Recipient email is required.");
                }
                if (string.IsNullOrWhiteSpace(resetCode))
                {
                    _logger.LogWarning("SendPasswordResetEmailAsync: resetCode is null or empty");
                    throw new ArgumentException("Reset code is required.");
                }
                var fromName = _config["Email:FromName"];
                var fromEmail = _config["Email:FromEmail"];
                var mailServer = _config["Email:MailServer"];
                var smtpUser = _config["Email:SMTPUser"];
                var smtpPassword = _config["Email:SMTPPassword"];
                var resetLink = _config["Email:ResetLink"];
                if (string.IsNullOrWhiteSpace(fromName) || string.IsNullOrWhiteSpace(fromEmail) || string.IsNullOrWhiteSpace(mailServer) || string.IsNullOrWhiteSpace(smtpUser) || string.IsNullOrWhiteSpace(smtpPassword) || string.IsNullOrWhiteSpace(resetLink))
                {
                    _logger.LogError("SendPasswordResetEmailAsync: Missing required email configuration");
                    throw new InvalidOperationException("Missing required email configuration.");
                }

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                emailMessage.To.Add(MailboxAddress.Parse(toEmail));
                emailMessage.Subject = "Reset your password";
                emailMessage.Body = new TextPart("html") { Text = $"<p>Click <a href=\"{resetLink}/?code={resetCode}\">here</a> to reset your password.</p>" };

                int port;
                port = int.TryParse(_config["Email:Port"], out port) ? port : 587;
                await _smtpClient.ConnectAsync(mailServer, port, SecureSocketOptions.StartTls);
                await _smtpClient.AuthenticateAsync(smtpUser, smtpPassword);
                await _smtpClient.SendAsync(emailMessage);
                await _smtpClient.DisconnectAsync(true);

                _logger.LogInformation("SendPasswordResetEmailAsync: Password reset email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendPasswordResetEmailAsync: Failed to send password reset email to {Email}", toEmail);
                throw;
            }
            finally
            {
                _logger.LogInformation("Exiting SendPasswordResetEmailAsync: toEmail={ToEmail}", toEmail);
            }
        }
    }
}