using Backend.Services.Impl;
using MailKit;
using MailKit.Net.Smtp;
using MimeKit;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class SmtpClientAdapterTests
    {
        [Fact]
        public async Task SmtpClientAdapter_DelegatesToSmtpClient_AllMethods()
        {
            // Arrange
            var smtpClientMock = new Mock<SmtpClient>();
            smtpClientMock.Setup(c => c.ConnectAsync("host", 25, MailKit.Security.SecureSocketOptions.StartTls, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask).Verifiable();
            smtpClientMock.Setup(c => c.SendAsync(It.IsAny<MimeMessage>(), It.IsAny<CancellationToken>(), It.IsAny<ITransferProgress>()))
                .Returns(Task.FromResult("")).Verifiable();
            smtpClientMock.Setup(c => c.DisconnectAsync(true, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask).Verifiable();

            var adapter = new SmtpClientAdapter(smtpClientMock.Object);

            // Act
            await adapter.ConnectAsync("host", 25, MailKit.Security.SecureSocketOptions.StartTls);
            await adapter.AuthenticateAsync("user", "pass");
            await adapter.SendAsync(new MimeMessage());
            await adapter.DisconnectAsync(true);
            adapter.Dispose();

            // Assert
            smtpClientMock.VerifyAll();
        }
    }
}
