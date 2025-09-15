using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Backend.Model;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Backend.DTOs;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers
{
    /// <summary>
    /// Initializes a new instance of the AuthController class.
    /// Handles user registration, login, logout, token management,
    /// and forgotten passwords.
    /// </summary>
    /// <param name="tokenService">Service for token operations.</param>
    /// <param name="userService">Service for user operations.</param>
    /// <param name="emailService">Service for email operations.</param>
    /// <param name="logger">Logger instance.</param>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController(ITokenService tokenService, IUserService userService, IEmailService emailService, ILogger<AuthController> logger) : ControllerBase
    {
        private readonly ITokenService _tokenService = tokenService;
        private readonly IUserService _userService = userService;
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<AuthController> _logger = logger;

        /// <summary>
        /// Registers a new user with the provided email and password.
        /// Returns access and refresh tokens if successful so user is immediately logged in.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <remarks>Returns access and refresh tokens if successful.</remarks>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> Register(DTOs.LoginRequest request)
        {
            const string method = nameof(Register);
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

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("{Method}: Model state is invalid.", method);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    _logger.LogDebug("{Method}: ModelState error: {Error}", method, error.ErrorMessage);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest(ModelState);
            }

            // Check if user already exists
            if (await _userService.GetByEmailAsync(request.Email) != null)
            {
                _logger.LogWarning("Registration failed: User with email {Email} already exists.", request.Email);
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return BadRequest("User already exists.");
            }

            User user;
            try
            {
                user = await _userService.CreateUserAsync(request.Email, request.Password);
                if (user == null)
                {
                    _logger.LogError("Registration failed: User creation returned null.");
                    _logger.LogDebug("Failed to create user with email {Email}.", request.Email);
                    _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                    return StatusCode(500, "Failed to create user.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Registration failed: Exception thrown: {ex}", ex);
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return StatusCode(500, ex.Message);
            }

            try
            {
                var result = await GenerateTokens(user);
                _logger.LogInformation("User registered successfully with email {Email}.", request.Email);
                _logger.LogDebug("Generated tokens for user with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Registration completed but token generation failed: {Error}", ex);
                _logger.LogDebug("Failed to generate tokens for user with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Register: email={Email}", request.Email);
                return StatusCode(500, "Registration completed successfully but failed to generate tokens: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs in a user with the provided email and password.
        /// </summary>
        /// <param name="request">JSON object with email and password.</param>
        /// <remarks>Returns a JSON object with access and refresh tokens if successful.</remarks>
        [HttpPost("login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> Login(DTOs.LoginRequest request)
        {
            const string method = nameof(Login);
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

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("{Method}: Model state is invalid.", method);
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                    _logger.LogDebug("{Method}: ModelState error: {Error}", method, error.ErrorMessage);
                _logger.LogInformation("{Method}: Exiting. email={Email}", method, request.Email);
                return BadRequest(ModelState);
            }

            User user;
            try
            {
                user = await _userService.GetByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Login failed: User with email {Email} not found.", request.Email);
                    _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                    return Unauthorized("Invalid email or password.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Login failed: Exception thrown: {ex}", ex);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return StatusCode(500, ex.Message);
            }

            try
            {
                if (!_userService.VerifyPasswordHash(request.Password, user))
                {
                    _logger.LogWarning("Login failed: Invalid password for user with email {Email}.", request.Email);
                    _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                    return Unauthorized("Invalid email or password.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Login failed verifying password: Exception thrown: {ex}", ex);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return StatusCode(500, ex.Message);
            }

            try
            {
                var result = await GenerateTokens(user);
                _logger.LogInformation("User logged in successfully with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Login failed: {Error}", ex.Message);
                _logger.LogDebug("Failed to generate tokens for user with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                return StatusCode(500, "Failed to generate tokens: " + ex.Message);
            }
        }

        /// <summary>
        /// Refreshes the access and refresh tokens using a valid refresh token.
        /// </summary>
        /// <param name="refreshToken">A refresh token string.</param>
        /// <remarks>Returns a JSON object with new access and refresh tokens if successful.</remarks>
        [Authorize]
        [HttpPost("refresh")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TokenResponse>> RefreshAsync([FromBody] string refreshToken)
        {
            const string method = nameof(RefreshAsync);
            _logger.LogInformation("{Method}: Entering. refreshToken={RefreshToken}", method, refreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("{Method}: Refresh token is null or empty.", method);
                _logger.LogInformation("{Method}: Exiting. refreshToken={RefreshToken}", method, refreshToken);
                return BadRequest("Refresh token is required.");
            }

            User? user = null;
            try
            {
                var oldRefreshToken = await _tokenService.FindRefreshTokenAsync(refreshToken);

                // if the refresh token is not found or is expired/revoked, return Unauthorized
                // this is a security measure to prevent token reuse
                if (oldRefreshToken == null || oldRefreshToken.Expires < DateTime.UtcNow || oldRefreshToken.IsRevoked)
                {
                    if (oldRefreshToken != null && oldRefreshToken.Expires < DateTime.UtcNow && !oldRefreshToken.IsRevoked)
                        await _tokenService.RevokeRefreshTokenAsync(oldRefreshToken);

                    _logger.LogWarning("Invalid or expired refresh token provided: {RefreshToken}", refreshToken);
                    return Unauthorized("Invalid refresh token.");
                }

                user = await _userService.GetByIdAsync(oldRefreshToken.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for refresh token: {RefreshToken}", refreshToken);
                    return Unauthorized("Invalid user.");
                }

                // Mark the old refresh token as revoked
                await _tokenService.RevokeRefreshTokenAsync(oldRefreshToken);
                _logger.LogDebug("Revoking old refresh token: {RefreshToken}", refreshToken);

                // Generate new tokens
                var result = await GenerateTokens(user);

                _logger.LogInformation("Tokens refreshed successfully for user with email {Email}.", user.Email);
                _logger.LogInformation("Exiting RefreshAsync: refreshToken={RefreshToken}", refreshToken);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to generate new tokens: {Error}", ex.Message);
                _logger.LogDebug("Failed to generate new tokens for user with email {Email}.", user?.Email);
                _logger.LogInformation("Exiting RefreshAsync: refreshToken={RefreshToken}", refreshToken);
                return StatusCode(500, "Failed to generate tokens: " + ex.Message);
            }
        }

        /// <summary>
        /// Logs out the user by revoking the provided refresh token.
        /// </summary>
        /// <param name="request">A refresh request containing a refresh token.</param>
        /// <remarks>Returns an OK status upon logout.</remarks>
        [Authorize]
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            const string method = nameof(Logout);
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

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("{Method} failed: Model state is invalid.", method);
                _logger.LogDebug("ModelState errors: {Errors}", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                _logger.LogDebug("Refresh token: {RefreshToken}", request);
                return BadRequest(ModelState);
            }

            try
            {
                var refreshTokenObj = await _tokenService.FindRefreshTokenAsync(request.RefreshToken);

                // If the refresh token is not found, we can still return OK
                // This is to ensure that the client can safely call logout without worrying about the token's existence
                // This is a common practice to avoid leaking information about token validity
                if (refreshTokenObj == null)
                    return Ok();

                await _tokenService.RevokeRefreshTokenAsync(refreshTokenObj);
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
        /// Allows an authenticated user to change their password
        /// </summary>
        /// <param name="request">JSON object with old password and new password.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [Authorize]
        [HttpPut("update-user")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserAsync(UserDto request)
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
                if (await _userService.UpdateUserDtoAsync(request))
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
                _logger.LogInformation(ex.Message);
                _logger.LogInformation("Exiting UpdateUserAsync: userId={UserId}", request.Id);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Allows an authenticated user to change their password
        /// </summary>
        /// <param name="request">JSON object with old password and new password.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [Authorize]
        [HttpPut("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            const string method = nameof(ChangePassword);
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
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword);
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
        /// <remarks>Returns an OK status on success or if email isn't recognized.</remarks>
        [HttpPost("forgot-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
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

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password request for non-existing email: {Email}", request.Email);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", request.Email);
                return Ok("If that email exists, a reset link has been sent."); // don’t reveal if email exists
            }

            try
            {
                var token = _tokenService.GenerateResetToken(user);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to generate reset token for user with email {Email}.", request.Email);
                    _logger.LogInformation("Exiting ForgotPassword: email={Email}", request.Email);
                    return StatusCode(500, "Failed to generate reset token.");
                }
                _logger.LogInformation("Reset token generated for user with email {Email}: {Token}", request.Email, token);
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
                _logger.LogInformation("Reset password email sent to {Email}.", user.Email);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", request.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reset password email to {Email}: {Message}", user.Email, ex.Message);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", request.Email);
                return StatusCode(500, "Failed to send reset email.");
            }

            return Ok("If that email exists, a reset link has been sent.");
        }

        /// <summary>
        /// Allows a valid user to reset their password.
        /// </summary>
        /// <param name="request">JSON object with reset token and email.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword(DTOs.ResetPasswordRequest request)
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

            var userId = _tokenService.ValidateResetToken(request.ResetCode);
            if (userId == null)
            {
                _logger.LogWarning("Reset password failed: Invalid or expired token.");
                _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                _logger.LogInformation("Exiting ResetPassword: token={Token}", request.ResetCode);
                return BadRequest("Invalid or expired token");
            }

            try
            {
                var success = await _userService.UpdatePasswordAsync(userId.Value, request.NewPassword);
                if (!success)
                {
                    _logger.LogError("Failed to reset password for user ID {UserId}.", userId);
                    _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                    _logger.LogInformation("Exiting ResetPassword: token={Token}", request.ResetCode);
                    return StatusCode(500, "Could not reset password");
                }
                _logger.LogInformation("Password reset successfully for user ID {UserId}.", userId);
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

        private async Task<TokenResponse> GenerateTokens(User user)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user, ip);
            var accessToken = _tokenService.GenerateAccessToken(user);

            if (accessToken == null || refreshToken == null)
            {
                _logger.LogError("Failed to generate access token or refresh token for user with email {Email}.", user.Email);
                _logger.LogDebug("AccessToken: {AccessToken}, RefreshToken: {RefreshToken}", accessToken, refreshToken);
                throw new ValidationException("Failed to generate access token or refresh token for user");
            }

            _logger.LogDebug("Generated access token and refresh token for user with email {Email}.", user.Email);
            _logger.LogDebug("AccessToken: {AccessToken}, RefreshToken: {RefreshToken}", accessToken, refreshToken.Token);

            return new TokenResponse
            {
                AccessToken = _tokenService.GenerateAccessToken(user),
                RefreshToken = refreshToken.Token!
            };
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}