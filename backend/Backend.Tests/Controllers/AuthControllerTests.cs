using Moq;
using Backend.Controllers;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity.Data;
using LoginRequest = Backend.Controllers.LoginRequest;
using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Controllers
{
    public class AuthControllerTests
    {
        [Fact]
        public async Task Refresh_ReturnsBadRequest_WhenRefreshTokenIMissing()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh(null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsBadRequest_WhenModelIsInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var emailService = new Mock<IEmailService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();

            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ModelState.AddModelError("RefreshToken", "Required");
            var result = await controller.Refresh(null!);

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenTokenInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync((RefreshToken)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("badtoken");

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenTokenRevoked()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var revokedToken = new RefreshToken { IsRevoked = true };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(revokedToken);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("badtoken");

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenTokenExpired()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var revokedToken = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(-1), IsRevoked = false };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(revokedToken);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("badtoken");

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsUnauthorized_WhenUserNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync((User)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("token");

            Assert.IsType<UnauthorizedObjectResult>(result);
        }

        [Fact]
        public async Task Refresh_ReturnsInternalServerError_WhenAccessTokenThrows()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            var user = new User { Id = 123, Email = "test@test.com" };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync(user);
            tokenService.Setup(s => s.GenerateAccessToken(user)).Throws(new Exception("Token generation failed"));
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("token");

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Refresh_ReturnsInternalServerError_WhenRefreshTokenThrows()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            var user = new User { Id = 123, Email = "test@test.com" };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync(user);
            tokenService.Setup(s => s.GenerateRefreshToken(user, "ip")).Throws(new Exception("Token generation failed"));
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("token");

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Refresh_ReturnsInternalServerError_WhenAccessTokenCannotBeGenerated()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            var user = new User { Id = 123, Email = "test@test.com" };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync(user);
            tokenService.Setup(s => s.GenerateAccessToken(user)).Returns((string)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("token");

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Refresh_ReturnsInternalServerError_WhenRefreshTokenCannotBeGenerated()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            var user = new User { Id = 123, Email = "test@test.com" };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync(user);
            tokenService.Setup(s => s.GenerateRefreshToken(user, "ip")).ReturnsAsync((RefreshToken)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Refresh("token");

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Refresh_ReturnsOk_WithNewTokens_WhenValid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var user = new User { Id = 123, Email = "test@example.com" };
            var refreshToken = new RefreshToken { UserId = 123, Expires = DateTime.UtcNow.AddMinutes(10), IsRevoked = false };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            userService.Setup(s => s.GetByIdAsync(refreshToken.UserId)).ReturnsAsync(user);
            var accesstoken = "access-token";
            tokenService.Setup(s => s.GenerateAccessToken(user)).Returns(accesstoken);
            var refreshTokenStr = "refresh-token";
            tokenService.Setup(s => s.GenerateRefreshToken(user, It.IsAny<string>())).ReturnsAsync(
                new RefreshToken { Token = refreshTokenStr });
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };
            var result = await controller.Refresh("token");

            var okResult = Assert.IsType<OkObjectResult>(result);
            TokenResponse? value = okResult.Value as TokenResponse;
            Assert.Equal(accesstoken, value?.AccessToken);
            Assert.Equal(refreshTokenStr, value?.RefreshToken);
            
            var dict = okResult.Value as IDictionary<string, object?> ??
                okResult.Value?.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(okResult.Value));
            Assert.True(dict?.ContainsKey("AccessToken"));
            Assert.True(dict?.ContainsKey("RefreshToken"));
            Assert.Equal(2, dict?.Count); // Only AccessToken and RefreshToken
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenEmailIsMissing()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Register(new RegisterRequest { Email = null!, Password = "pass" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenPasswordIsMissing()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = null! });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenModelIsInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ModelState.AddModelError("RefreshToken", "Required");
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = "pass" });

            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Register_ReturnsBadRequest_WhenUserExists()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(new User());
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = "pass" });

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("User already exists.", badRequest.Value);
        }

        [Fact]
        public async Task Register_ReturnsServerError_WhenUserCreationFails()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            userService.Setup(s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync((User)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = "pass" });

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsServerError_WhenGeneratingTokensFails()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            userService.Setup(s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(
                new User { Id = 1, Email = "test@test.com" });
            tokenService.Setup(s => s.GenerateAccessToken(It.IsAny<User>())).Throws(new Exception("Token generation failed"));
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = "pass" });

            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenUserCreatedAndTokensGenerated()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var user = new User { Id = 1, Email = "test@example.com" };
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            userService.Setup(s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(user);
            var accessTokenStr = "access-token";
            var refreshTokenStr = "refresh-token";
            tokenService.Setup(s => s.GenerateAccessToken(user)).Returns(accessTokenStr);
            tokenService.Setup(s => s.GenerateRefreshToken(user, It.IsAny<string>())).ReturnsAsync(
                new RefreshToken { Token = refreshTokenStr });
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.Register(new RegisterRequest { Email = "test@example.com", Password = "pass" });

            var okResult = Assert.IsType<OkObjectResult>(result);
            TokenResponse? value = okResult.Value as TokenResponse;
            Assert.Equal(accessTokenStr, value?.AccessToken);
            Assert.Equal(refreshTokenStr, value?.RefreshToken);
            
            var dict = okResult.Value as IDictionary<string, object?> ??
                okResult.Value?.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(okResult.Value));
            Assert.True(dict?.ContainsKey("AccessToken"));
            Assert.True(dict?.ContainsKey("RefreshToken"));
            Assert.Equal(2, dict?.Count); // Only AccessToken and RefreshToken
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenEmailIsMissing()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;
            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Login(new LoginRequest { Email = null!, Password = "pass" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenPasswordIsMissing()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;
            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = null! });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenModelStateIsInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;
            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ModelState.AddModelError("Email", "Required");
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = "pass" });
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenUserNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();;
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = "pass" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid email or password.", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordInvalid()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var user = new User { Id = 1, Email = "test@example.com" };
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            userService.Setup(s => s.VerifyPasswordHash(It.IsAny<string>(), user)).Returns(false);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = "wrong" });

            var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Equal("Invalid email or password.", unauthorized.Value);
        }

        [Fact]
        public async Task Login_ReturnsServerError_WhenGeneratingTokensFails()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var user = new User { Id = 1, Email = "test@example.com" };
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            userService.Setup(s => s.VerifyPasswordHash(It.IsAny<string>(), user)).Returns(true);
            tokenService.Setup(s => s.GenerateAccessToken(user)).Throws(new Exception("Token generation failed"));
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = "right" });

            var unauthorized = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, unauthorized.StatusCode);
        }

        [Fact]
        public async Task Login_ReturnsOk_WhenValidCredentials()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var user = new User { Id = 1, Email = "test@example.com" };
            userService.Setup(s => s.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(user);
            userService.Setup(s => s.VerifyPasswordHash(It.IsAny<string>(), user)).Returns(true);
            var accessTokenStr = "access-token";
            var refreshTokenStr = "refresh-token";
            tokenService.Setup(s => s.GenerateAccessToken(user)).Returns(accessTokenStr);
            tokenService.Setup(s => s.GenerateRefreshToken(user, It.IsAny<string>())).ReturnsAsync(
                new RefreshToken { Token = refreshTokenStr });
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
            var result = await controller.Login(new LoginRequest { Email = "test@example.com", Password = "pass" });

            var okResult = Assert.IsType<OkObjectResult>(result);
            TokenResponse? value = okResult.Value as TokenResponse;
            Assert.Equal(accessTokenStr, value?.AccessToken);
            Assert.Equal(refreshTokenStr, value?.RefreshToken);

            var dict = okResult.Value as IDictionary<string, object?> ??
                okResult.Value?.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(okResult.Value));
            Assert.True(dict?.ContainsKey("AccessToken"));
            Assert.True(dict?.ContainsKey("RefreshToken"));
            Assert.Equal(2, dict?.Count); // Only AccessToken and RefreshToken
        }

        [Fact]
        public async Task Logout_ReturnsOk_WhenTokenNotFound()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync((RefreshToken)null!);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Logout("badtoken");

            Assert.IsType<OkResult>(result);
        }

        [Fact]
        public async Task Logout_RevokesTokenAndReturnsOk()
        {
            var tokenService = new Mock<ITokenService>();
            var userService = new Mock<IUserService>();
            var refreshToken = new RefreshToken { Token = "token" };
            tokenService.Setup(s => s.FindRefreshToken(It.IsAny<string>())).ReturnsAsync(refreshToken);
            tokenService.Setup(s => s.RevokeRefreshToken(refreshToken)).Returns(Task.CompletedTask);
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthController>();;

            var emailService = new Mock<IEmailService>();
            var controller = new AuthController(tokenService.Object, userService.Object, emailService.Object, logger);
            var result = await controller.Logout("token");

            Assert.IsType<OkResult>(result);
        }
    }
}
