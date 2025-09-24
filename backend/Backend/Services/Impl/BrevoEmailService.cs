using Backend.Model;
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
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _client.Dispose();
        }
    }

    public class BrevoEmailService(IConfiguration config, ILogger<BrevoEmailService> logger, ISmtpClient smtpClient) : IEmailService
    {
        private readonly IConfiguration _config = config;
        private readonly ILogger<BrevoEmailService> _logger = logger;
        private readonly ISmtpClient _smtpClient = smtpClient;

        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="user">The current user;</param>
        /// <param name="resetCode">The password reset link to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task SendPasswordResetEmailAsync(User user, string resetCode)
        {
            const string method = nameof(SendPasswordResetEmailAsync);
            _logger.LogInformation("{Method}: Entering", method);

            if (user == null)
            {
                _logger.LogWarning("{Metehod}: User is required but is null.", method);
                throw new ArgumentException("User cannot be null");
            }

            try
            {
                if (string.IsNullOrWhiteSpace(user.Email))
                {
                    _logger.LogWarning("{Method}: toEmail is null or empty. Id={Id}", method, user.Id);
                    throw new ArgumentException("Recipient email is required.");
                }
                if (string.IsNullOrWhiteSpace(resetCode))
                {
                    _logger.LogWarning("{Method}: resetCode is null or empty. Id = {Id}", method, user.Id);
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
                    _logger.LogError("{Method}: Missing required email configuration. Id={Id}", method, user.Id);
                    _logger.LogDebug("{Method}: FromName={FromName}", method, fromName);
                    _logger.LogDebug("{Method}: FromEmail={FromEmail}", method, fromEmail);
                    _logger.LogDebug("{Method}: MailServer={MailServer}", method, mailServer);
                    _logger.LogDebug("{Method}: SMTPUser={SmtpUser}", method, smtpUser);
                    _logger.LogDebug("{Method}: ResetLink={ResetLink}", method, resetLink);
                    throw new InvalidOperationException("Missing required email configuration.");
                }

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(fromName, fromEmail));
                emailMessage.To.Add(MailboxAddress.Parse(user.Email));
                emailMessage.Subject = "Reset your password";
                emailMessage.Body = new TextPart("html") { Text = $"<p>Click <a href=\"{resetLink}/?code={resetCode}\">here</a> to reset your password.</p>" };

                int port;
                port = int.TryParse(_config["Email:Port"], out port) ? port : 587;
                await _smtpClient.ConnectAsync(mailServer, port, SecureSocketOptions.StartTls);
                await _smtpClient.AuthenticateAsync(smtpUser, smtpPassword);
                await _smtpClient.SendAsync(emailMessage);
                await _smtpClient.DisconnectAsync(true);

                _logger.LogInformation("{Method}: Password reset email sent to Id={Id}", method, user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Failed to send password reset email to Id={Id}", method, user.Id);
                throw;
            }
            finally
            {
                _logger.LogInformation("Exiting {Method}: Id={Id}", method, user.Id);
            }
        }
    }
}