using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Backend.Controllers
{
    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// Handles user registration, login, logout, token management,
    /// and forgotten passwords.
    /// </summary>
    /// <param name="userService">Service for user operations.</param>
    /// <param name="logger">Logger instance.</param>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(IUserService userService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly ILogger<AuthController> _logger = logger;

        /// <summary>
        /// Registers a new user with the provided email and password.
        /// Returns access and refresh tokens if successful so user is immediately logged in.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns access and refresh tokens if successful.</remarks>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> RegisterAsync(DTOs.LoginRequest request, CancellationToken ct)
        {
            const string method = nameof(RegisterAsync);
            _logger.LogInformation("{Method}: Entering. email={Email}", method, request?.Email);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request?.Email);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("{Method}: Email is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("{Method}: Password is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest("Password is required.");
            }

            try
            {
                var result = await _userService.RegisterNewUserAsync(request, GetIP(), ct);
                if (result == null)
                {
                    _logger.LogError("Registration failed: User creation returned null tokens.");
                    _logger.LogDebug("Failed to create user with email {Email}.", request.Email);
                    _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                    return StatusCode(500, "Failed to create user.");
                }

                _logger.LogInformation("User registered successfully with email {Email}.", request.Email);
                _logger.LogDebug("Generated tokens for user with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed");
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Logs in a user with the provided email and password.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns a JSON object with access and refresh tokens if successful.</remarks>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> LoginAsync(DTOs.LoginRequest request, CancellationToken ct)
        {
            const string method = nameof(LoginAsync);
            _logger.LogInformation("{Method}: Entering. email={Email}", method, request?.Email);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request?.Email);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("{Method}: Email is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest("Email is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                _logger.LogWarning("{Method}: Password is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest("Password is required.");
            }

            try
            {
                var result = await _userService.LoginAsync(request, GetIP(), ct);
                if (result == null)
                {
                    _logger.LogError("Login failed: LoginAsync returned null tokens.");
                    _logger.LogDebug("Failed to log in user with email {Email}.", request.Email);
                    _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                    return Unauthorized("Invalid email or password.");
                }

                _logger.LogInformation("User logged in successfully with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed");
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the access and refresh tokens using a valid refresh token.
        /// </summary>
        /// <param name="request">A refresh token string.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns a JSON object with new access and refresh tokens if successful.</remarks>
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> RefreshAsync([FromBody] DTOs.RefreshRequest request, CancellationToken ct)
        {
            const string method = nameof(RefreshAsync);
            _logger.LogInformation("{Method}: Entering. refreshToken={RefreshToken}", method, request.RefreshToken);
            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("{Method}: Refresh token is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. refreshToken={RefreshToken}", method, request.RefreshToken);
                return BadRequest("Refresh token is required.");
            }

            try
            {
                var result = await _userService.RefreshTokensAsync(request.RefreshToken, GetIP(), ct);

                _logger.LogInformation("Exiting RefreshAsync: refreshToken={RefreshToken}", request.RefreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate new tokens: {Error}", ex.Message);
                _logger.LogInformation("Exiting RefreshAsync: refreshToken={RefreshToken}", request.RefreshToken);
                return StatusCode(500, "Failed to generate tokens: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs out the user by revoking the provided refresh token.
        /// </summary>
        /// <param name="request">A refresh request containing a refresh token.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns an OK status upon logout.</remarks>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> LogoutAsync([FromBody] DTOs.RefreshRequest request, CancellationToken ct)
        {
            const string method = nameof(LogoutAsync);
            _logger.LogInformation("{Method}: Entering. refreshToken={RefreshToken}", method, request?.RefreshToken);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. refreshToken={RefreshToken}", method, request?.RefreshToken);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.RefreshToken))
            {
                _logger.LogWarning("{Method}: Refresh token is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. refreshToken={RefreshToken}", method, request.RefreshToken);
                return BadRequest("Refresh token is required.");
            }

            try
            {
                await _userService.LogoutAsync(request.RefreshToken, ct);

                _logger.LogInformation("Exiting Logout: refreshToken={RefreshToken}", request.RefreshToken);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Exiting Logout: refreshToken={RefreshToken}", request?.RefreshToken);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Allows an authenticated user to change their email or any future user details.
        /// </summary>
        /// <param name="request">User object DTO with all the users details to be updated.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [Authorize]
        [HttpPut("update-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserAsync(UserDto request, CancellationToken ct)
        {
            const string method = nameof(UpdateUserAsync);
            _logger.LogInformation("{Method}: Entering. userId={UserId}", method, request?.Id);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, request?.Id);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("{Method}: Email is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, request.Id);
                return BadRequest("Email is required.");
            }

            try
            {
                if (await _userService.UpdateUserDtoAsync(request, ct))
                {
                    _logger.LogInformation("Exiting UpdateUserAsync: userId={UserId}", request.Id);
                    return Ok();
                }
                else
                {
                    _logger.LogInformation("Exiting UpdateUserAsync: userId={UserId}", request.Id);
                    return BadRequest("Unable to update user.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "UpdateUserAsync: Exception thrown.");
                _logger.LogInformation("Exiting UpdateUserAsync: userId={UserId}", request.Id);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Allows an authenticated user to change their password
        /// </summary>
        /// <param name="request">JSON object with old password and new password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [Authorize]
        [HttpPut("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
        {
            const string method = nameof(ChangePasswordAsync);
            var userId = GetUserId();
            _logger.LogInformation("{Method}: Entering. userId={UserId}", method, userId);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.OldPassword))
            {
                _logger.LogWarning("{Method}: Old password is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
                return BadRequest("Old password is required.");
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                _logger.LogWarning("{Method}: New password is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
                return BadRequest("New password is required.");
            }

            try
            {
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, ct);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Change password unauthorized for user ID {UserId}: {Message}", userId, ex.Message);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
                return Unauthorized("Old password is incorrect.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Change password failed for user ID {UserId}: {Message}", userId, ex.Message);
                _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
                return StatusCode(500, "An error occurred while changing the password. " + ex.Message);
            }

            _logger.LogInformation("Password changed successfully for user ID {UserId}.", userId);
            _logger.LogInformation("{Method}: Exiting. userId={UserId}", method, userId);
            return Ok("Password updated successfully");
        }

        /// <summary>
        /// Allows a user to request to reset their password. Sends an email to a valid user to allow them to reset password.
        /// </summary>
        /// <param name="request">JSON object with email.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns an OK status on success or if email isn't recognized.</remarks>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
        {
            const string method = nameof(ForgotPassword);
            _logger.LogInformation("{Method}: Entering. email={Email}", method, request?.Email);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request?.Email);
                return Ok("If that email exists, a reset link has been sent.");
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                _logger.LogWarning("{Method}: Email is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return Ok("If that email exists, a reset link has been sent.");
            }

            try
            {
                await _userService.ForgotPasswordAsync(request.Email, ct);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(ex, "Exiting ForgotPassword: email={Email}", request.Email);
                return StatusCode(500, "Failed to send reset email.");
            }

            return Ok("If that email exists, a reset link has been sent.");
        }

        /// <summary>
        /// Allows a valid user to reset their password.
        /// </summary>
        /// <param name="request">JSON object with reset token and email.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword(DTOs.ResetPasswordRequest request, CancellationToken ct)
        {
            const string method = nameof(ResetPassword);
            _logger.LogInformation("{Method}: Entering. token={Token}", method, request?.ResetCode);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, request?.ResetCode);
                return BadRequest("Request object is required.");
            }

            if (string.IsNullOrWhiteSpace(request.ResetCode))
            {
                _logger.LogWarning("{Method}: Reset token is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, request.ResetCode);
                return BadRequest("Invalid or expired token");
            }

            try
            {
                var success = await _userService.ResetPasswordAsync(request, ct);
                if (!success)
                {
                    _logger.LogError("Failed to reset password for user.");
                    _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                    _logger.LogInformation("Exiting ResetPassword: token={Token}", request.ResetCode);
                    return StatusCode(500, "Could not reset password");
                }

                _logger.LogInformation("Password reset successfully for user.");
                _logger.LogInformation("Exiting ResetPassword: token={Token}", request.ResetCode);
                return Ok("Password has been reset successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to reset password for user, exception thrown. {ex}", ex.Message);
                _logger.LogInformation("Exiting ResetPassword: token={Token}", request?.ResetCode);
                return StatusCode(500, "Could not reset password: {0}" + ex.Message);
            }
        }

        private string GetIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}