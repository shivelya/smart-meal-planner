using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Model;
using System.ComponentModel.DataAnnotations;

namespace Backend.Services.Impl
{
    public class UserService(PlannerContext context, ITokenService tokenService, IEmailService emailService, ILogger<UserService> logger) : IUserService
    {
        private readonly PlannerContext _context = context;
        private readonly ITokenService _tokenService = tokenService;
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger<UserService> _logger = logger;

        public async Task<TokenResponse> RegisterNewUserAsync(LoginRequest request, string ip, CancellationToken ct)
        {
            _logger.LogInformation("Entering CreateUserAsync: email={Email}", request.Email);

            // Check if user already exists
            if (await GetByEmailAsync(request.Email) != null)
            {
                _logger.LogInformation("CreateUserAsync: Attempt to create a user that already exists: {Email}", request.Email);
                throw new InvalidOperationException("User already exists.");
            }

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            _logger.LogDebug("CreateUserAsync: Password hashed for user: {Email}; hash: {Hash}", request.Email, hashedPassword);

            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = hashedPassword
                };

                await _context.Users.AddAsync(user, ct);
                await _context.SaveChangesAsync(ct);

                var result = await GenerateTokensAsync(user, ip, ct);

                _logger.LogInformation("CreateUserAsync: User created successfully: {User}", user);
                _logger.LogInformation("Exiting CreateUserAsync: email={Email}, userId={UserId}", request.Email, user.Id);

                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken ct)
        {
            _logger.LogInformation("Entering ChangePasswordAsync: userId={UserId}", userId);
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("ChangePasswordAsync: User not found with ID: {UserId}", userId);
                throw new ArgumentException("User not found.");
            }

            if (!VerifyPasswordHash(oldPassword, user))
            {
                _logger.LogWarning("ChangePasswordAsync: Old password does not match for user ID: {UserId}", userId);
                throw new ValidationException("Old password is incorrect.");
            }

            // Hash the new password
            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordHash = newHashedPassword;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("ChangePasswordAsync: Password changed successfully for user ID: {UserId}", userId);
            _logger.LogInformation("Exiting ChangePasswordAsync: userId={UserId}", userId);
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
        {
            var userId = _tokenService.ValidateResetToken(request.ResetCode);
            _logger.LogInformation("Entering UpdatePasswordAsync: userId={UserId}", userId);
            if (userId == null)
            {
                _logger.LogWarning("Reset password failed: Invalid or expired token.");
                _logger.LogDebug("Reset token: {Token}", request.ResetCode);
                _logger.LogInformation("Exiting ResetPassword: token={Token}", request.ResetCode);
                throw new ArgumentException("Invalid or expired token");
            }

            var user = await GetByIdAsync(userId.Value);
            if (user == null)
            {
                _logger.LogWarning("UpdatePasswordAsync: User not found with ID: {UserId}", userId);
                _logger.LogInformation("Exiting UpdatePasswordAsync: userId={UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("UpdatePasswordAsync: Password updated successfully for user ID: {UserId}", userId);
            _logger.LogInformation("Exiting UpdatePasswordAsync: userId={UserId}", userId);
            return true;
        }

        public async Task UpdateUserDtoAsync(UserDto userDto, CancellationToken ct)
        {
            _logger.LogInformation("Entering UpdateUserDtoAsync: userId={UserId}", userDto.Id);
            var user = await GetByIdAsync(userDto.Id);
            if (user == null)
            {
                _logger.LogWarning("UpdateUserDtoAsync: User not found with ID: {UserId}", userDto.Id);
                throw new ArgumentException($"User not found with ID: {userDto.Id}");
            }

            user.Email = userDto.Email;
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("UpdateUserDtoAsync: User updated for userId={UserId}", userDto.Id);
            _logger.LogInformation("Exiting UpdateUserDtoAsync: userId={UserId}", userDto.Id);
        }

        public async Task<TokenResponse> RefreshTokensAsync(string refreshToken, string ip, CancellationToken ct)
        {
            _logger.LogInformation("Entering RefreshAsync with refreshToken: {RefreshToken}", refreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("RefreshAsync: Refresh token is null or empty.");
                throw new ArgumentException("Refresh token is required.");
            }

            var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var oldRefreshToken = await _tokenService.VerifyRefreshTokenAsync(refreshToken, ct);

                if (oldRefreshToken == null)
                {
                    _logger.LogWarning("RefreshTokensAsync: invalid refresh token provided. token={Token}", refreshToken);
                    throw new ValidationException("Invalid refresh token provided.");
                }

                var user = await GetByIdAsync(oldRefreshToken.UserId);
                if (user == null)
                {
                    _logger.LogWarning("User not found for refresh token: {RefreshToken}", refreshToken);
                    throw new ArgumentException("Invalid user.");
                }

                // Mark the old refresh token as revoked
                await _tokenService.RevokeRefreshTokenAsync(refreshToken, ct);
                _logger.LogDebug("Revoking old refresh token: {RefreshToken}", refreshToken);

                // Generate new tokens
                var result = await GenerateTokensAsync(user, ip, ct);
                _logger.LogInformation("Generated new tokens for userId={UserId} using refresh token.", user.Id);
                _logger.LogDebug("New AccessToken: {AccessToken}, New RefreshToken: {RefreshToken}", result.AccessToken, result.RefreshToken);
                _logger.LogInformation("Exiting RefreshAsync: userId={UserId}", user.Id);

                await transaction.CommitAsync(ct);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task LogoutAsync(string refreshToken, CancellationToken ct)
        {
            _logger.LogInformation("Entering Logout with refreshToken: {RefreshToken}", refreshToken);
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                _logger.LogWarning("Logout: Refresh token is null or empty.");
                throw new ArgumentException("Refresh token is required.");
            }

            // If the refresh token is not found, we can still return OK
            // This is to ensure that the client can safely call logout without worrying about the token's existence
            // This is a common practice to avoid leaking information about token validity
            await _tokenService.RevokeRefreshTokenAsync(refreshToken, ct);
            _logger.LogInformation("Exiting Logout: refreshToken={RefreshToken}", refreshToken);
        }

        public async Task ForgotPasswordAsync(string email, CancellationToken ct)
        {
            _logger.LogInformation("Entering ForgotPassword: email={Email}", email);
            if (string.IsNullOrWhiteSpace(email))
            {
                _logger.LogWarning("ForgotPassword: Email is null or empty.");
                throw new ArgumentException("Email is required.");
            }

            var user = await GetByEmailAsync(email);
            if (user == null)
            {
                _logger.LogInformation("Forgot password request for non-existing email: {Email}", email);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", email);

                // don’t reveal if email exists
                return;
            }

            var transaction = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                var token = _tokenService.GenerateResetToken(user);
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to generate reset token for user with email {Email}.", email);
                    _logger.LogInformation("Exiting ForgotPassword: email={Email}", email);
                    throw new InvalidOperationException("Failed to generate reset token.");
                }

                _logger.LogInformation("Reset token generated for user with email {Email}: {Token}", email, token);
                await _emailService.SendPasswordResetEmailAsync(user.Email, token);
                await transaction.CommitAsync(ct);

                _logger.LogInformation("Password reset email sent to {Email}.", email);
                _logger.LogInformation("Exiting ForgotPassword: email={Email}", email);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<TokenResponse> LoginAsync(LoginRequest request, string ip, CancellationToken ct)
        {
            _logger.LogInformation("Entering Login: email={Email}", request.Email);

            var user = await GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User with email {Email} not found.", request.Email);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                throw new ValidationException($"User with email {request.Email} not found.");
            }

            if (!VerifyPasswordHash(request.Password, user))
            {
                _logger.LogWarning("Login failed: Invalid password for user with email {Email}.", request.Email);
                _logger.LogInformation("Exiting Login: email={Email}", request.Email);
                throw new ValidationException($"Invalid password for email {request.Email}");
            }

            var result = await GenerateTokensAsync(user, ip, ct);
            _logger.LogInformation("User logged in successfully with email {Email}.", request.Email);
            _logger.LogInformation("Exiting Login: email={Email}", request.Email);
            return result;
        }

        private async Task<User> GetByEmailAsync(string email)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                _logger.LogInformation("GetByEmailAsync: User not found with email: {Email}", email);
                return null!;
            }

            return user;
        }

        private async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogInformation("GetByIdAsync: User not found with ID: {Id}", id);
                return null!;
            }

            _logger.LogInformation("GetByIdAsync: User found with ID: {Id}", id);
            return user;
        }

        private bool VerifyPasswordHash(string password, User user)
        {
            // Verify the password against the stored hash
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("VerifyPasswordHash: User or password hash is null or empty.");
                _logger.LogInformation("Exiting VerifyPasswordHash: userId={UserId}", user?.Id);
                return false;
            }

            var result = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            return result;
        }

        private async Task<TokenResponse> GenerateTokensAsync(User user, string ip, CancellationToken ct)
        {
            var refreshToken = await _tokenService.GenerateRefreshTokenAsync(user, ip, ct);
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
                AccessToken = accessToken,
                RefreshToken = refreshToken.Token!
            };
        }
    }
}