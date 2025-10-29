using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public class AuthController(IUserService userService, ILogger<AuthController> logger) : PlannerControllerBase(logger)
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
        public async Task<ActionResult<TokenResponse>> RegisterAsync(LoginRequest request, CancellationToken ct)
        {
            const string method = nameof(RegisterAsync);
            _logger.LogInformation("{Method}: Entering", method);

            if (CheckForNull(method, request, nameof(request)) is { } check) return check;

            request.Email = SanitizeInput(request.Email);
            request.Password = SanitizeInput(request.Password);

            if (CheckForNullOrWhitespace(method, request.Email, nameof(request.Email)) is { } check2) return check2;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.Password, nameof(request.Password), request.Password) is { } check3) return check3;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                try
                {
                    var result = await _userService.RegisterNewUserAsync(request, GetIP(), ct);
                    if (ResultNullCheck(method, result) is { } check4) return check4;

                    _logger.LogInformation("User registered successfully.");
                    _logger.LogDebug("Generated tokens for user.");
                    return Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    // user is already created
                    _logger.LogWarning(ex, "RegisterAsync: User already exists.");
                    _logger.LogInformation("Exiting Register: userId={UserId}", GetUserId());
                    return StatusCode(409, "User account already exists.");
                }
            });
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
        public async Task<ActionResult<TokenResponse>> LoginAsync(LoginRequest request, CancellationToken ct)
        {
            const string method = nameof(LoginAsync);
            _logger.LogInformation("{Method}: Entering", method);

            if (CheckForNull(method, request, nameof(request)) is { } check) return check;

            request.Email = SanitizeInput(request.Email);
            request.Password = SanitizeInput(request.Password);

            if (CheckForNullOrWhitespace(method, request.Email, nameof(request.Email)) is { } check2) return check2;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.Password, nameof(request.Password)) is { } check3) return check3;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var result = await _userService.LoginAsync(request, GetIP(), ct);
                if (ResultNullCheck(method, result) is { } check4) return check4;

                _logger.LogInformation("User {UserId} logged in successfully with email.", GetUserId());
                return Ok(result);
            });
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
        public async Task<ActionResult<TokenResponse>> RefreshAsync([FromBody] RefreshRequest request, CancellationToken ct)
        {
            request.RefreshToken = SanitizeInput(request.RefreshToken);
            const string method = nameof(RefreshAsync);
            _logger.LogInformation("{Method}: Entering. refreshToken={RefreshToken}", method, request.RefreshToken);

            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request?.RefreshToken, nameof(request.RefreshToken)) is { } check2) return check2;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var result = await _userService.RefreshTokensAsync(request?.RefreshToken!, GetIP(), ct);
                return ResultNullCheck(method, result, request?.RefreshToken) is { } check3 ? check3 : Ok(result);
            }, request?.RefreshToken);
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
        public async Task<IActionResult> LogoutAsync([FromBody] RefreshRequest request, CancellationToken ct)
        {
            const string method = nameof(LogoutAsync);
            _logger.LogInformation("{Method}: Entering", method);
            var userId = GetUserId();

            if (CheckForNull(method, request, nameof(request)) is { } result) return result;

            request.RefreshToken = SanitizeInput(request.RefreshToken);

#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.RefreshToken, nameof(request.RefreshToken)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                await _userService.LogoutAsync(request.RefreshToken, ct);
                return Ok();
            });
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
            _logger.LogInformation("{Method}: Entering", method);

            var userId = GetUserId();
            if (CheckForNull(method, request, nameof(request)) is { } result) return result;

            request.Email = SanitizeInput(request.Email);

            if (CheckForNullOrWhitespace(method, request.Email, nameof(request.Email)) is { } check) return check;

            if (userId != request.Id)
            {
                _logger.LogWarning("{Method}: Attempt to update different user.", method);
                _logger.LogWarning("{Method}: Attempt to update different user. Current user: {CurrentId}  Passed in user: {RequestId}", method, User, request.Id);
                return BadRequest("Id must match current user.");
            }

            return await TryCallToServiceAsync(method, async () =>
            {
                await _userService.UpdateUserDtoAsync(request, ct);
                return Ok();
            });
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

            if (CheckForNull(method, request, nameof(request)) is { } result) return result;

            request.NewPassword = SanitizeInput(request.NewPassword);
            request.OldPassword = SanitizeInput(request.OldPassword);

            if (CheckForNullOrWhitespace(method, request.OldPassword, "Old password") is { } check) return check;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.NewPassword, "New password") is { } check2) return check2;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                await _userService.ChangePasswordAsync(userId, request.OldPassword, request.NewPassword, ct);
                _logger.LogInformation("Password changed successfully for user ID {UserId}.", userId);
                return Ok();
            });
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
            _logger.LogInformation("{Method}: Entering.", method);

            if (CheckForNull(method, request, nameof(request), ret: () => Ok("If that email exists, a reset link has been sent.")) is { } result) return result;

            request.Email = SanitizeInput(request.Email);

#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.Email, nameof(request.Email), request.Email,
                () => Ok("If that email exists, a reset link has been sent.")) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                await _userService.ForgotPasswordAsync(request.Email!, ct);
                return Ok();
            }, request.Email);
        }

        /// <summary>
        /// Allows a valid user to reset their password.
        /// </summary>
        /// <param name="request">JSON object with reset token and email.</param>
        /// <param name="ct">JSON object with reset token and email.</param>
        /// <remarks>Returns an OK status upon success.</remarks>
        [HttpPost("reset-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
        {
            const string method = nameof(ResetPassword);
            _logger.LogInformation("{Method}: Entering", method);

            if (CheckForNull(method, request, nameof(request)) is { } result) return result;

            request.NewPassword = SanitizeInput(request.NewPassword);
            request.ResetCode = SanitizeInput(request.ResetCode);

#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForNullOrWhitespace(method, request.ResetCode, nameof(request.ResetCode)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var success = await _userService.ResetPasswordAsync(request, ct);
                if (ResultNullCheck<bool?>(method, success ? success : null, request.ResetCode) is { } check4) return check4;

                _logger.LogInformation("Password reset successfully for user.");
                return Ok();
            }, request.ResetCode);
        }

        private string GetIP()
        {
            return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
    }
}