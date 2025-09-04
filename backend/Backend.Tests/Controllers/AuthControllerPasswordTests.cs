using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ResetPasswordRequest = Backend.DTOs.ResetPasswordRequest;

namespace Backend.Tests.Controllers
{
    public class AuthControllerNewMethodsTests
    {
        private AuthController CreateController(
            Mock<ITokenService>? tokenService = null,
            Mock<IUserService>? userService = null,
            Mock<IEmailService>? emailService = null,
            ILogger<AuthController>? logger = null,
            ClaimsPrincipal? user = null)
        {
            tokenService ??= new Mock<ITokenService>();
            userService ??= new Mock<IUserService>();
            emailService ??= new Mock<IEmailService>();
            logger ??= new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            if (user != null)
                controller.ControllerContext.HttpContext.User = user;
            return controller;
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenModelStateInvalid()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("OldPassword", "Required");
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsBadRequest_WhenPasswordsMissing()
        {
            var controller = CreateController();
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "", NewPassword = null! });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsUnauthorized_WhenUserIdMissing()
        {
            var controller = CreateController();
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
            Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ChangePassword_ReturnsUnauthorized_WhenOldPasswordIncorrect()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new UnauthorizedAccessException());
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
            var controller = CreateController(userService: userService, user: claims);
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Old password is incorrect.", unauthorized.Value);
        }

        [Fact]
        public async Task ChangePassword_ReturnsServerError_OnException()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("fail"));
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
            var controller = CreateController(userService: userService, user: claims);
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
            var error = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, error.StatusCode);
            Assert.Contains("fail", error.Value?.ToString());
        }

        [Fact]
        public async Task ChangePassword_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync("user1", "old", "new"))
                .Returns(Task.CompletedTask);
            var claims = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "user1") }));
            var controller = CreateController(userService: userService, user: claims);
            var result = await controller.ChangePassword(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Password updated successfully", ok.Value?.ToString());
        }

        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenUserNotFound()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            var controller = CreateController(userService: userService);
            var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "notfound@example.com" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("reset link has been sent", ok.Value?.ToString());
        }

        [Fact]
        public async Task ForgotPassword_ReturnsServerError_WhenTokenGenerationFails()
        {
            var userService = new Mock<IUserService>();
            var tokenService = new Mock<ITokenService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = 1, Email = "user@example.com" });
            tokenService.Setup(s => s.GenerateResetToken(It.IsAny<User>())).Returns((string)null!);
            var controller = CreateController(userService: userService, tokenService: tokenService);
            var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "user@example.com" });
            var error = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, error.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsServerError_WhenEmailSendFails()
        {
            var userService = new Mock<IUserService>();
            var tokenService = new Mock<ITokenService>();
            var emailService = new Mock<IEmailService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = 1, Email = "user@example.com" });
            tokenService.Setup(s => s.GenerateResetToken(It.IsAny<User>())).Returns("token123");
            emailService.Setup(s => s.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("smtp fail"));
            var controller = CreateController(userService: userService, tokenService: tokenService, emailService: emailService);
            var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "user@example.com" });
            var error = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, error.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            var tokenService = new Mock<ITokenService>();
            var emailService = new Mock<IEmailService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User { Id = 1, Email = "user@example.com" });
            tokenService.Setup(s => s.GenerateResetToken(It.IsAny<User>())).Returns("token123");
            emailService.Setup(s => s.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var controller = CreateController(userService: userService, tokenService: tokenService, emailService: emailService);
            var result = await controller.ForgotPassword(new ForgotPasswordRequest { Email = "user@example.com" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("reset link has been sent", ok.Value?.ToString());
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenTokenInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            tokenService.Setup(s => s.ValidateResetToken(It.IsAny<string>())).Returns((int?)null);
            var controller = CreateController(tokenService: tokenService);
            var result = await controller.ResetPassword(new ResetPasswordRequest { ResetCode = "badtoken", NewPassword = "newpass" });
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid or expired token", badRequest.Value?.ToString());
        }

        [Fact]
        public async Task ResetPassword_ReturnsServerError_WhenUpdateFails()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            tokenService.Setup(s => s.ValidateResetToken(It.IsAny<string>())).Returns(1);
            userService.Setup(s => s.UpdatePasswordAsync(1, It.IsAny<string>())).ReturnsAsync(false);
            var controller = CreateController(tokenService: tokenService, userService: userService);
            var result = await controller.ResetPassword(new ResetPasswordRequest { ResetCode = "goodtoken", NewPassword = "newpass" });
            var error = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, error.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenSuccess()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            tokenService.Setup(s => s.ValidateResetToken(It.IsAny<string>())).Returns(1);
            userService.Setup(s => s.UpdatePasswordAsync(1, It.IsAny<string>())).ReturnsAsync(true);
            var controller = CreateController(tokenService: tokenService, userService: userService);
            var result = await controller.ResetPassword(new ResetPasswordRequest { ResetCode = "goodtoken", NewPassword = "newpass" });
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("Password has been reset successfully", ok.Value?.ToString());
        }
    }
}
