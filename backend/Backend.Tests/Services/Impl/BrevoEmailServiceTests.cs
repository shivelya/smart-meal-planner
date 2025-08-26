using Backend.Services.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using MimeKit;

namespace Backend.Tests.Services.Impl
{
    public class BrevoEmailServiceTests
    {
        [Fact]
        public async Task SendPasswordResetEmailAsync_SendsEmail_WithCorrectParameters()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:FromName"]).Returns("Test Sender");
            configMock.Setup(c => c["Email:FromEmail"]).Returns("sender@example.com");
            configMock.Setup(c => c["Email:ResetLink"]).Returns("https://resetlink");
            configMock.Setup(c => c["Email:MailServer"]).Returns("smtp.example.com");
            configMock.Setup(c => c["Email:Port"]).Returns("587");
            configMock.Setup(c => c["Email:SMTPUser"]).Returns("user");
            configMock.Setup(c => c["Email:SMTPPassword"]).Returns("pass");

            var loggerMock = new Mock<ILogger<BrevoEmailService>>();

            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock.Setup(c => c.ConnectAsync("smtp.example.com", 587, MailKit.Security.SecureSocketOptions.StartTls)).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.AuthenticateAsync("user", "pass")).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>())).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.DisconnectAsync(true)).Returns(Task.CompletedTask);

            var service = new BrevoEmailService(configMock.Object, loggerMock.Object, smtpClientMock.Object);

            await service.SendPasswordResetEmailAsync("recipient@example.com", "resetcode123");

            smtpClientMock.Verify(c => c.ConnectAsync("smtp.example.com", 587, MailKit.Security.SecureSocketOptions.StartTls), Times.Once);
            smtpClientMock.Verify(c => c.AuthenticateAsync("user", "pass"), Times.Once);
            smtpClientMock.Verify(c => c.SendAsync(It.IsAny<MimeMessage>()), Times.Once);
            smtpClientMock.Verify(c => c.DisconnectAsync(true), Times.Once);
        }

        [Fact]
        public async Task SendPasswordResetEmailAsync_UsesDefaultPort_WhenPortIsMissingOrInvalid()
        {
            var configMock = new Mock<IConfiguration>();
            configMock.Setup(c => c["Email:FromName"]).Returns("Test Sender");
            configMock.Setup(c => c["Email:FromEmail"]).Returns("sender@example.com");
            configMock.Setup(c => c["Email:ResetLink"]).Returns("https://resetlink");
            configMock.Setup(c => c["Email:MailServer"]).Returns("smtp.example.com");
            configMock.Setup(c => c["Email:Port"]).Returns((string?)null); // missing port
            configMock.Setup(c => c["Email:SMTPUser"]).Returns("user");
            configMock.Setup(c => c["Email:SMTPPassword"]).Returns("pass");

            var loggerMock = new Mock<ILogger<BrevoEmailService>>();
            var smtpClientMock = new Mock<ISmtpClient>();
            smtpClientMock.Setup(c => c.ConnectAsync("smtp.example.com", 587, MailKit.Security.SecureSocketOptions.StartTls)).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.AuthenticateAsync("user", "pass")).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>())).Returns(Task.CompletedTask);
            smtpClientMock.Setup(c => c.DisconnectAsync(true)).Returns(Task.CompletedTask);

            var service = new BrevoEmailService(configMock.Object, loggerMock.Object, smtpClientMock.Object);
            await service.SendPasswordResetEmailAsync("recipient@example.com", "resetcode123");
            smtpClientMock.Verify(c => c.ConnectAsync("smtp.example.com", 587, MailKit.Security.SecureSocketOptions.StartTls), Times.Once);
        }
    }
}