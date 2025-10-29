using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Moq;
using Backend.Controllers;
using Backend.Services;
using Backend.DTOs;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace Backend.Tests.Controllers
{
    public class AuthControllerTests
    {
        private static AuthController GetController(Mock<IUserService>? userService = null)
        {
            userService ??= new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();
            var controller = new AuthController(userService.Object, logger);
            var rand = new Random();
            var userId = rand.Next(1, 1000);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
            return controller;
        }

        // ChangePasswordAsync
        [Fact]
        public async Task ChangePasswordAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.ChangePasswordAsync(null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsBadRequest_WhenOldPasswordMissing()
        {
            var controller = GetController();
            var result = await controller.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = null!, NewPassword = "new" }, CancellationToken.None);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Old password is required.", badRequest.Value);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsBadRequest_WhenNewPasswordMissing()
        {
            var controller = GetController();
            var result = await controller.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "old", NewPassword = null! }, CancellationToken.None);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("New password is required.", badRequest.Value);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsUnauthorized_WhenUnauthorizedAccessException()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new ValidationException());
            var controller = GetController(userService);
            // Simulate user id in claims
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]));
            var result = await controller.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }, CancellationToken.None);
            var unauthorized = Assert.IsType<UnauthorizedResult>(result);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsServerError_WhenException()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]));
            var result = await controller.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ChangePasswordAsync_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ChangePasswordAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).Returns(Task.CompletedTask);
            var controller = GetController(userService);
            controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]));
            var result = await controller.ChangePasswordAsync(new ChangePasswordRequest { OldPassword = "old", NewPassword = "new" }, CancellationToken.None);
            var ok = Assert.IsType<OkResult>(result);
        }

        // ForgotPassword
        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.ForgotPassword(null!, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("If that email exists, a reset link has been sent.", ok.Value);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenEmailMissing()
        {
            var controller = GetController();
            var result = await controller.ForgotPassword(new Backend.DTOs.ForgotPasswordRequest { Email = null! }, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result);
            Assert.Equal("If that email exists, a reset link has been sent.", ok.Value);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ForgotPasswordAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.CompletedTask);
            var controller = GetController(userService);
            var result = await controller.ForgotPassword(new Backend.DTOs.ForgotPasswordRequest { Email = "a" }, CancellationToken.None);
            var ok = Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task ForgotPassword_ReturnsServerError_WhenException()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ForgotPasswordAsync(It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.ForgotPassword(new Backend.DTOs.ForgotPasswordRequest { Email = "a" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        // ResetPassword
        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.ResetPassword(null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsBadRequest_WhenTokenMissing()
        {
            var controller = GetController();
            var result = await controller.ResetPassword(new Backend.DTOs.ResetPasswordRequest { ResetCode = null!, NewPassword = "new" }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task ResetPassword_ReturnsServerError_WhenServiceThrows()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ResetPasswordAsync(It.IsAny<Backend.DTOs.ResetPasswordRequest>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.ResetPassword(new Backend.DTOs.ResetPasswordRequest { ResetCode = "token", NewPassword = "new" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_ReturnsServerError_WhenResetFails()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ResetPasswordAsync(It.IsAny<Backend.DTOs.ResetPasswordRequest>(), CancellationToken.None)).ReturnsAsync(false);
            var controller = GetController(userService);
            var result = await controller.ResetPassword(new Backend.DTOs.ResetPasswordRequest { ResetCode = "token", NewPassword = "new" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ResetPassword_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.ResetPasswordAsync(It.IsAny<Backend.DTOs.ResetPasswordRequest>(), CancellationToken.None)).ReturnsAsync(true);
            var controller = GetController(userService);
            var result = await controller.ResetPassword(new Backend.DTOs.ResetPasswordRequest { ResetCode = "token", NewPassword = "new" }, CancellationToken.None);
            var ok = Assert.IsType<OkResult>(result);
        }

        // RegisterAsync
        [Fact]
        public async Task RegisterAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.RegisterAsync(null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsBadRequest_WhenEmailMissing()
        {
            var controller = GetController();
            var result = await controller.RegisterAsync(new Backend.DTOs.LoginRequest { Email = null!, Password = "b" }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsBadRequest_WhenPasswordMissing()
        {
            var controller = GetController();
            var result = await controller.RegisterAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = null! }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsServerError_WhenServiceThrows()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.RegisterNewUserAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.RegisterAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsServerError_WhenNullReturned()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.RegisterNewUserAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((TokenResponse)null!);
            var controller = GetController(userService);
            var result = await controller.RegisterAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task RegisterAsync_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            var tokens = new TokenResponse { AccessToken = "access", RefreshToken = "refresh" };
            userService.Setup(s => s.RegisterNewUserAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(tokens);
            var controller = GetController(userService);
            var result = await controller.RegisterAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<TokenResponse>(ok.Value);
            Assert.Equal("access", value.AccessToken);
            Assert.Equal("refresh", value.RefreshToken);
        }
        // LoginAsync
        [Fact]
        public async Task LoginAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.LoginAsync(null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task LoginAsync_ReturnsBadRequest_WhenEmailMissing()
        {
            var controller = GetController();
            var result = await controller.LoginAsync(new Backend.DTOs.LoginRequest { Email = null!, Password = "b" }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task LoginAsync_ReturnsBadRequest_WhenPasswordMissing()
        {
            var controller = GetController();
            var result = await controller.LoginAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = null! }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task LoginAsync_ReturnsServerError_WhenServiceThrows()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.LoginAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.LoginAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task LoginAsync_ReturnsUnauthorized_WhenNullReturned()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.LoginAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync((TokenResponse)null!);
            var controller = GetController(userService);
            var result = await controller.LoginAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var unauthorized = Assert.IsType<StatusCodeResult>(result.Result);
        }

        [Fact]
        public async Task LoginAsync_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            var tokens = new TokenResponse { AccessToken = "access", RefreshToken = "refresh" };
            userService.Setup(s => s.LoginAsync(It.IsAny<Backend.DTOs.LoginRequest>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(tokens);
            var controller = GetController(userService);
            var result = await controller.LoginAsync(new Backend.DTOs.LoginRequest { Email = "a", Password = "b" }, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<TokenResponse>(ok.Value);
            Assert.Equal("access", value.AccessToken);
            Assert.Equal("refresh", value.RefreshToken);
        }

        // RefreshAsync
        [Fact]
        public async Task RefreshAsync_ReturnsBadRequest_WhenTokenMissing()
        {
            var controller = GetController();
            var result = await controller.RefreshAsync(new Backend.DTOs.RefreshRequest { RefreshToken = null!}, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task RefreshAsync_ReturnsServerError_WhenServiceThrows()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.RefreshTokensAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.RefreshAsync(new Backend.DTOs.RefreshRequest { RefreshToken = "token" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task RefreshAsync_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            var tokens = new TokenResponse { AccessToken = "access", RefreshToken = "refresh" };
            userService.Setup(s => s.RefreshTokensAsync(It.IsAny<string>(), It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(tokens);
            var controller = GetController(userService);
            var result = await controller.RefreshAsync(new Backend.DTOs.RefreshRequest { RefreshToken = "token" }, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<TokenResponse>(ok.Value);
            Assert.Equal("access", value.AccessToken);
            Assert.Equal("refresh", value.RefreshToken);
        }

        // LogoutAsync
        [Fact]
        public async Task LogoutAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.LogoutAsync(null!, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LogoutAsync_ReturnsBadRequest_WhenTokenMissing()
        {
            var controller = GetController();
            var result = await controller.LogoutAsync(new Backend.DTOs.RefreshRequest { RefreshToken = null! }, CancellationToken.None);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task LogoutAsync_ReturnsServerError_WhenServiceThrows()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.LogoutAsync(It.IsAny<string>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(userService);
            var result = await controller.LogoutAsync(new Backend.DTOs.RefreshRequest { RefreshToken = "token" }, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task LogoutAsync_ReturnsOk_WhenSuccess()
        {
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.LogoutAsync(It.IsAny<string>(), CancellationToken.None)).Returns(Task.CompletedTask);
            var controller = GetController(userService);
            var result = await controller.LogoutAsync(new Backend.DTOs.RefreshRequest { RefreshToken = "token" }, CancellationToken.None);
            Assert.IsType<OkResult>(result);
        }
    }
}
