using Backend.DTOs;
using Backend.Model;
using Backend.Services;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.ComponentModel.DataAnnotations;

namespace Backend.Tests.Services.Impl
{
    public class UserServiceTests
    {
        private PlannerContext context = null!;
        private UserService CreateService(
            Mock<ITokenService>? tokenService = null,
            Mock<IEmailService>? emailService = null,
            ILogger<UserService>? logger = null)
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            context = new PlannerContext(options, config, new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>());
            var tokenSvc = tokenService ?? new Mock<ITokenService>();
            var emailSvc = emailService ?? new Mock<IEmailService>();
            var log = logger ?? new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserService>();
            return new UserService(context, tokenSvc.Object, emailSvc.Object, log);
        }

        [Fact]
        public async Task RegisterNewUserAsync_CreatesUserAndReturnsTokens()
        {
            var tokenService = new Mock<ITokenService>();
            var service = CreateService(tokenService: tokenService);
            var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
            var ip = "127.0.0.1";
            tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
            tokenService.Setup(t => t.GenerateRefreshTokenAsync(It.IsAny<User>(), ip)).ReturnsAsync(new RefreshToken { Token = "refresh-token" });

            var result = await service.RegisterNewUserAsync(request, ip);

            Assert.Single(context.Users);
            var user = context.Users.First();
            Assert.Equal("test@example.com", user.Email);
            Assert.NotEqual(BCrypt.Net.BCrypt.HashPassword("password123"), user.PasswordHash); // Password should be hashed

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task RegisterNewUserAsync_Throws_WhenUserExists()
        {
            var service = CreateService();
            var request = new LoginRequest { Email = "test@example.com", Password = "password123" };
            var ip = "127.0.0.1";

            context.Users.Add(new User { Email = "test@example.com", PasswordHash = "hashed" });
            context.SaveChanges();

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.RegisterNewUserAsync(request, ip));
        }

        [Fact]
        public async Task LoginAsync_ReturnsTokens_WhenCredentialsValid()
        {
            var tokenService = new Mock<ITokenService>();
            var service = CreateService(tokenService: tokenService);
            var ip = "127.0.0.1";
            tokenService.Setup(t => t.GenerateAccessToken(It.IsAny<User>())).Returns("access-token");
            tokenService.Setup(t => t.GenerateRefreshTokenAsync(It.IsAny<User>(), ip)).ReturnsAsync(new RefreshToken { Token = "refresh-token" });
            var login = new LoginRequest { Email = "test@example.com", Password = "password123" };

            context.Users.Add(new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") });
            context.SaveChanges();

            var result = await service.LoginAsync(login, ip);

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.AccessToken));
            Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        }

        [Fact]
        public async Task LoginAsync_Throws_WhenUserNotFound()
        {
            var service = CreateService();
            var login = new LoginRequest { Email = "notfound@example.com", Password = "password123" };
            var ip = "127.0.0.1";
            await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(login, ip));
        }

        [Fact]
        public async Task LoginAsync_Throws_WhenPasswordIncorrect()
        {
            var service = CreateService();
            var ip = "127.0.0.1";
            var login = new LoginRequest { Email = "test@example.com", Password = "wrongpassword" };

            context.Users.Add(new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("hashed") });
            context.SaveChanges();

            await Assert.ThrowsAsync<ArgumentException>(() => service.LoginAsync(login, ip));
        }

        [Fact]
        public async Task ChangePasswordAsync_ChangesPassword_WhenOldPasswordCorrect()
        {
            var service = CreateService();
            var user = new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            context.Users.Add(user);
            context.SaveChanges();

            await service.ChangePasswordAsync(user.Id, "oldpass", "newpass");

            BCrypt.Net.BCrypt.Verify("newpass", context.Users.First().PasswordHash);
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenOldPasswordIncorrect()
        {
            var service = CreateService();
            var user = new User { Email = "test@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            context.Users.Add(user);
            context.SaveChanges();

            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.ChangePasswordAsync(user.Id, "wrongpass", "newpass"));
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenUserNotFound()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() => service.ChangePasswordAsync(9999, "oldpass", "newpass"));
        }

        [Fact]
        public async Task ForgotPasswordAsync_SendsEmail_WhenUserExists()
        {
            var emailService = new Mock<IEmailService>();
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(t => t.GenerateResetToken(It.IsAny<User>())).Returns("reset-token");
            emailService.Setup(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask).Verifiable();
            var service = CreateService(tokenService: tokenService, emailService: emailService);
            context.Users.Add(new User { Email = "forgot@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("hashed") });
            context.SaveChanges();

            await service.ForgotPasswordAsync("forgot@example.com");

            emailService.Verify(e => e.SendPasswordResetEmailAsync("forgot@example.com", "reset-token"), Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsync_DoesNothing_WhenUserNotFound()
        {
            var emailService = new Mock<IEmailService>();
            var tokenService = new Mock<ITokenService>();
            var service = CreateService(tokenService: tokenService, emailService: emailService);
            await service.ForgotPasswordAsync("notfound@example.com");
            emailService.Verify(e => e.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ResetPasswordAsync_UpdatesPassword_WhenTokenValid()
        {
            var tokenService = new Mock<ITokenService>();
            var emailService = new Mock<IEmailService>();
            var service = CreateService(tokenService: tokenService, emailService: emailService);
            var userEntity = new User { Email = "resetpass@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass") };
            context.Users.Add(userEntity);
            context.SaveChanges();

            tokenService.Setup(t => t.ValidateResetToken(It.IsAny<string>())).Returns(userEntity.Id);
            var request = new ResetPasswordRequest { ResetCode = "reset-token", NewPassword = "newpass" };

            var result = await service.ResetPasswordAsync(request);

            Assert.True(result);
            Assert.True(BCrypt.Net.BCrypt.Verify("newpass", context.Users.First(u => u.Id == userEntity.Id).PasswordHash));
        }

        [Fact]
        public async Task ResetPasswordAsync_Throws_WhenTokenInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(t => t.ValidateResetToken(It.IsAny<string>())).Returns((int?)null);
            var service = CreateService(tokenService: tokenService);
            var request = new ResetPasswordRequest { ResetCode = "bad-token", NewPassword = "newpass" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.ResetPasswordAsync(request));
        }

        [Fact]
        public async Task UpdateUserDtoAsync_UpdatesUser_WhenExists()
        {
            var service = CreateService();
            var user = new User { Email = "updateuser@example.com", PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123") };
            context.Users.Add(user);
            context.SaveChanges();
            var dto = new UserDto { Id = user.Id, Email = "updated@example.com" };

            var result = await service.UpdateUserDtoAsync(dto);

            Assert.True(result);
            context.Users.First(u => u.Id == user.Id).Email.Equals("updated@example.com");
        }

        [Fact]
        public async Task UpdateUserDtoAsync_Throws_WhenUserNotFound()
        {
            var service = CreateService();
            var dto = new UserDto { Id = 9999, Email = "notfound@example.com" };
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateUserDtoAsync(dto));
        }

        [Fact]
        public async Task RefreshTokensAsync_Throws_WhenTokenMissing()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.RefreshTokensAsync(null!, "ip"));
            Assert.Equal("Refresh token is required.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokensAsync_Throws_WhenTokenNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken)null!);
            var service = CreateService(tokenService: tokenService);
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.RefreshTokensAsync("badtoken", "ip"));
            Assert.Equal("Invalid refresh token.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokensAsync_Throws_WhenTokenExpiredOrRevoked()
        {
            var tokenService = new Mock<ITokenService>();
            var expiredToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(-1), IsRevoked = false };
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(expiredToken);
            tokenService.Setup(t => t.RevokeRefreshTokenAsync(expiredToken)).Returns(Task.CompletedTask);
            var service = CreateService(tokenService: tokenService);
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.RefreshTokensAsync("expired", "ip"));
            Assert.Equal("Invalid refresh token.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokensAsync_Throws_WhenUserNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            var validToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false, UserId = 9999 };
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(validToken);
            tokenService.Setup(t => t.RevokeRefreshTokenAsync(validToken)).Returns(Task.CompletedTask);
            var service = CreateService(tokenService: tokenService);
            // No user with id 9999 in context
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.RefreshTokensAsync("valid", "ip"));
            Assert.Equal("Invalid user.", ex.Message);
        }

        [Fact]
        public async Task RefreshTokensAsync_ReturnsTokens_WhenValid()
        {
            var tokenService = new Mock<ITokenService>();
            var user = new User { Email = "refresh@example.com", PasswordHash = "hash" };
            var service = CreateService(tokenService: tokenService);
            context.Users.Add(user);
            context.SaveChanges();
            var validToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false, UserId = user.Id };
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(validToken);
            tokenService.Setup(t => t.RevokeRefreshTokenAsync(validToken)).Returns(Task.CompletedTask);
            tokenService.Setup(t => t.GenerateAccessToken(user)).Returns("access-token");
            tokenService.Setup(t => t.GenerateRefreshTokenAsync(user, "ip")).ReturnsAsync(new RefreshToken { Token = "new-refresh" });

            var result = await service.RefreshTokensAsync("valid", "ip");

            Assert.NotNull(result);
            Assert.Equal("access-token", result.AccessToken);
            Assert.Equal("new-refresh", result.RefreshToken);
        }

        [Fact]
        public async Task LogoutAsync_Throws_WhenTokenMissing()
        {
            var service = CreateService();
            var ex = await Assert.ThrowsAsync<ValidationException>(() => service.LogoutAsync(null!));
            Assert.Equal("Refresh token is required.", ex.Message);
        }

        [Fact]
        public async Task LogoutAsync_DoesNothing_WhenTokenNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync((RefreshToken)null!);
            var service = CreateService(tokenService: tokenService);
            // Should not throw
            await service.LogoutAsync("notfound");
            tokenService.Verify(t => t.RevokeRefreshTokenAsync(It.IsAny<RefreshToken>()), Times.Never);
        }

        [Fact]
        public async Task LogoutAsync_RevokesToken_WhenTokenFound()
        {
            var tokenService = new Mock<ITokenService>();
            var token = new RefreshToken { Token = "tok", Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            tokenService.Setup(t => t.FindRefreshTokenAsync(It.IsAny<string>())).ReturnsAsync(token);
            tokenService.Setup(t => t.RevokeRefreshTokenAsync(token)).Returns(Task.CompletedTask).Verifiable();
            var service = CreateService(tokenService: tokenService);
            await service.LogoutAsync("tok");
            tokenService.Verify(t => t.RevokeRefreshTokenAsync(token), Times.Once);
        }
    }
}
