using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Backend.Model;
using Backend.Services;
using Serilog;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ITokenService tokenService, IUserService userService, IEmailService emailService, ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _userService = userService;
            _emailService = emailService;
            _logger = logger;
        }

        // POST: api/auth/register
        /// <summary>
        /// Registers a new user with the provided email and password.
        /// Returns access and refresh tokens if successful so user is immediately logged in.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <returns>Return access and refresh tokens if successful.</returns>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            _logger.LogDebug("Request: {Request}", request);
            if (request.Email == null || request.Password == null)
            {
                _logger.LogWarning("Registration failed: Email or password is null.");
                return BadRequest("Email and password are required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Registration failed: Model state is invalid.");
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            // Check if user already exists
            if (await _userService.GetByEmailAsync(request.Email) != null)
            {
                _logger.LogWarning("Registration failed: User with email {Email} already exists.", request.Email);
                return BadRequest("User already exists.");
            }

            var user = await _userService.CreateUserAsync(request.Email, request.Password);
            if (user == null)
            {
                _logger.LogError("Registration failed: User creation returned null.");
                _logger.LogDebug("Failed to create user with email {Email}.", request.Email);
                return StatusCode(500, "Failed to create user.");
            }

            var result = await GenerateTokens(user);
            if (result.Error != null)
            {
                _logger.LogError("Registration failed: {Error}", result.Error);
                _logger.LogDebug("Failed to generate tokens for user with email {Email}.", request.Email);
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");
            }

            _logger.LogInformation("User registered successfully with email {Email}.", request.Email);
            _logger.LogDebug("Generated tokens for user with email {Email}.", request.Email);

            // Return the tokens
            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken?.Token!,
            });
        }

        // POST: api/auth/login
        /// <summary>
        /// Logs in a user with the provided email and password.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <returns>JSON object with access and refresh tokens if successful.</returns>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            _logger.LogDebug("Login request: {Request}", request);
            if (request.Email == null || request.Password == null)
            {
                _logger.LogWarning("Login failed: Email or password is null.");
                return BadRequest("Email and password are required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Login failed: Model state is invalid.");
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", request.Email);
                return Unauthorized("Invalid email or password.");
            }

            if (!_userService.VerifyPasswordHash(request.Password, user))
            {
                _logger.LogWarning("Login failed: Invalid password for user with email {Email}.", request.Email);
                return Unauthorized("Invalid email or password.");
            }

            var result = await GenerateTokens(user);
            if (result.Error != null)
            {
                _logger.LogError("Login failed: {Error}", result.Error);
                _logger.LogDebug("Failed to generate tokens for user with email {Email}.", request.Email);
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");
            }

            _logger.LogInformation("User logged in successfully with email {Email}.", request.Email);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken?.Token!,
            });
        }

        // POST: api/auth/refresh
        /// <summary>
        /// Refreshes the access and refresh tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">A refresh token string.</param>
        /// <returns>JSON object with new access and refresh tokens if successful.</returns>
        [Authorize]
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Refresh([FromBody] string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Null refresh token provided.");
                return BadRequest("Refresh token is required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model state is invalid for refresh token request.");
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogDebug("Refresh token: {RefreshToken}", refreshToken);
                return BadRequest(ModelState);
            }

            var oldRefreshToken = await _tokenService.FindRefreshToken(refreshToken);

            // if the refresh token is not found or is expired/revoked, return Unauthorized
            // this is a security measure to prevent token reuse
            if (oldRefreshToken == null || oldRefreshToken.Expires < DateTime.UtcNow
                || oldRefreshToken.IsRevoked)
            {
                _logger.LogWarning("Invalid or expired refresh token provided: {RefreshToken}", refreshToken);
                return Unauthorized("Invalid refresh token.");
            }

            var user = await _userService.GetByIdAsync(oldRefreshToken.UserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token: {RefreshToken}", refreshToken);
                return Unauthorized("Invalid user.");
            }

            // Mark the old refresh token as revoked
            oldRefreshToken.IsRevoked = true;
            _logger.LogDebug("Revoking old refresh token: {RefreshToken}", refreshToken);

            // Generate new tokens
            var result = await GenerateTokens(user);
            if (result.Error != null)
            {
                _logger.LogError("Failed to generate new tokens: {Error}", result.Error);
                _logger.LogDebug("Failed to generate new tokens for user with email {Email}.", user.Email);
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");
            }

            _logger.LogInformation("Tokens refreshed successfully for user with email {Email}.", user.Email);

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken.Token!
            });
        }

        // POST: api/auth/logout
        /// <summary>
        /// Logs out the user by revoking the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">A refresh token string.</param>
        /// <returns>OK status upon logout.</returns>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("Logout failed: Null refresh token provided.");
                return BadRequest("Refresh token is required.");
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Logout failed: Model state is invalid.");
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogDebug("Refresh token: {RefreshToken}", refreshToken);
                return BadRequest(ModelState);
            }

            var refreshTokenObj = await _tokenService.FindRefreshToken(refreshToken);

            // If the refresh token is not found, we can still return OK
            // This is to ensure that the client can safely call logout without worrying about the token's existence
            // This is a common practice to avoid leaking information about token validity
            if (refreshTokenObj == null)
                return Ok();

            await _tokenService.RevokeRefreshToken(refreshTokenObj);
            return Ok();
        }

        // PUT: api/auth/change-password
        /// <summary>
        /// Allows an authenticated user to change their password
        /// </summary>
        /// <param name="request">JSON object with old password and new password.</param>
        /// <returns>OK status upon success.</returns>
        [Authorize]
        [HttpPut("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Change password failed: Model state is invalid.");
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return BadRequest(ModelState);
            }

            if (string.IsNullOrEmpty(request.OldPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                _logger.LogWarning("Change password failed: Old password or new password is null or empty.");
                return BadRequest("Old password and new password are required.");
            }


            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Change password failed: User ID not found in claims.");
                return Unauthorized();
            }

            try
            {
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Change password unauthorized for user ID {UserId}: {Message}", userId, ex.Message);
                return Unauthorized("Old password is incorrect.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password failed for user ID {UserId}: {Message}", userId, ex.Message);
                return StatusCode(500, "An error occurred while changing the password. " + ex.Message);
            }

            _logger.LogInformation("Password changed successfully for user ID {UserId}.", userId);

            return Ok(new { message = "Password updated successfully" });
        }

        /// <summary>
        /// Allows a user to request to reset their password. Sends an email to a valid user to allow them to reset password.
        /// </summary>
        /// <param name="request">JSON object with email.</param>
        /// <returns>OK status on success.</returns>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password request for non-existing email: {Email}", request.Email);
                return Ok("If that email exists, a reset link has been sent."); // don’t reveal if email exists
            }

            var token = _tokenService.GenerateResetToken(user);
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Failed to generate reset token for user with email {Email}.", request.Email);
                return StatusCode(500, "Failed to generate reset token.");
            }

            _logger.LogInformation("Reset token generated for user with email {Email}: {Token}", request.Email, token);

            try
            {
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
                _logger.LogInformation("Reset password email sent to {Email}.", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset password email to {Email}: {Message}", user.Email, ex.Message);
                return StatusCode(500, "Failed to send reset email.");
            }

            return Ok("If that email exists, a reset link has been sent.");
        }

        /// <summary>
        /// Allows a valid user to reset their password.
        /// </summary>
        /// <param name="request">JSON object with reset token and email.</param>
        /// <returns>OK status upon success.</returns>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var userId = _tokenService.ValidateResetToken(request.ResetCode);
            if (userId == null)
            {
                _logger.LogWarning("Reset password failed: Invalid or expired token.");
                _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                return BadRequest("Invalid or expired token");
            }

            var success = await _userService.UpdatePasswordAsync(userId.Value, request.NewPassword);
            if (!success)
            {
                _logger.LogError("Failed to reset password for user ID {UserId}.", userId);
                _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                return StatusCode(500, "Could not reset password");
            }

            _logger.LogInformation("Password reset successfully for user ID {UserId}.", userId);
            return Ok("Password has been reset successfully.");
        }

        private async Task<TokenGenerateResult> GenerateTokens(User user)
        {
            var result = new TokenGenerateResult();
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                result.AccessToken = _tokenService.GenerateAccessToken(user);
                result.RefreshToken = await _tokenService.GenerateRefreshToken(user, ip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user with email {Email}: {message}", user.Email, ex.Message);
                result.Error = StatusCode(500, $"Internal server error: Failed to generate token.");
                return result;
            }

            if (result.AccessToken == null || result.RefreshToken == null)
            {
                _logger.LogError("Failed to generate access token or refresh token for user with email {Email}.", user.Email);
                _logger.LogDebug("AccessToken: {AccessToken}, RefreshToken: {RefreshToken}", result.AccessToken, result.RefreshToken);
                result.Error = StatusCode(500, "Failed to generate token.");
                return result;
            }

            _logger.LogDebug("Generated access token and refresh token for user with email {Email}.", user.Email);
            _logger.LogDebug("AccessToken: {AccessToken}, RefreshToken: {RefreshToken}", result.AccessToken, result.RefreshToken.Token);
            return result;
        }
    }

    public class TokenGenerateResult
    {
        public IActionResult? Error { get; set; }
        public string AccessToken { get; set; } = null!;
        public RefreshToken RefreshToken { get; set; } = null!;
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class TokenResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string ResetCode { get; set; }
        public required string NewPassword { get; set; }
    }
}