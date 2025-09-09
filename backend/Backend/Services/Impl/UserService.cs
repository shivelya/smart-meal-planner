using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Model;

namespace Backend.Services.Impl
{
    public class UserService(PlannerContext context, ILogger<UserService> logger) : IUserService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<UserService> _logger = logger;

        /// <summary>
        /// Creates a new user with the specified email and password.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>The created user.</returns>
        public async Task<User> CreateUserAsync(string email, string password)
        {
            // Check if user already exists
            if (await GetByEmailAsync(email) != null)
            {
                _logger.LogInformation("Attempt to create a user that already exists: {Email}", email);
                throw new InvalidOperationException("User already exists.");
            }

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            _logger.LogDebug("Password hashed for user: {Email}; hash: {hash}", email, hashedPassword);

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {user}", user);

            return user;
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public async Task<User> GetByEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email) ?? null!;
            if (user == null)
            {
                _logger.LogInformation("User not found with email: {Email}", email);
                return null!;
            }

            return user;
        }

    /// <summary>
    /// Retrieves a user by their unique ID.
    /// </summary>
    /// <param name="id">The user's unique identifier.</param>
    /// <returns>The user if found, otherwise null.</returns>
    public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id) ?? null!;
            if (user == null)
            {
                _logger.LogInformation("User not found with ID: {Id}", id);
                return null!;
            }

            return user;
        }

    /// <summary>
    /// Verifies the password hash for the specified user.
    /// </summary>
    /// <param name="password">The password to verify.</param>
    /// <param name="user">The user whose password hash to verify.</param>
    /// <returns>True if the password matches, otherwise false.</returns>
    public bool VerifyPasswordHash(string password, User user)
        {
            // Verify the password against the stored hash
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("User or password hash is null or empty.");
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

    /// <summary>
    /// Changes the password for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="oldPassword">The user's current password.</param>
    /// <param name="newPassword">The user's new password.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ChangePasswordAsync(string userId, string oldPassword, string newPassword)
        {
            if (!int.TryParse(userId, out int id))
            {
                _logger.LogWarning("Invalid user ID format: {UserId}", userId);
                throw new ArgumentException("Invalid user ID format.");
            }

            var user = await GetByIdAsync(id);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                throw new InvalidOperationException("User not found.");
            }

            if (!VerifyPasswordHash(oldPassword, user))
            {
                _logger.LogWarning("Old password does not match for user ID: {UserId}", userId);
                throw new UnauthorizedAccessException("Old password is incorrect.");
            }

            // Hash the new password
            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordHash = newHashedPassword;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password changed successfully for user ID: {UserId}", userId);
        }

    /// <summary>
    /// Updates the password for the specified user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="newPassword">The new password to set.</param>
    /// <returns>True if the update was successful, otherwise false.</returns>
    public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found with ID: {UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Password updated successfully for user ID: {UserId}", userId);
            return true;
        }

    /// <summary>
    /// Updates the user DTO information.
    /// </summary>
    /// <param name="userDto">The user DTO to update.</param>
    /// <returns>The updated user DTO.</returns>
    public Task<UserDto> UpdateUserDtoAsync(UserDto userDto)
        {
            throw new NotImplementedException();
        }
    }
}