using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Model;
using System.ComponentModel.DataAnnotations;

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
            _logger.LogInformation("Entering CreateUserAsync: email={Email}", email);
            // Check if user already exists
            if (await GetByEmailAsync(email) != null)
            {
                _logger.LogInformation("CreateUserAsync: Attempt to create a user that already exists: {Email}", email);
                throw new InvalidOperationException("User already exists.");
            }

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            _logger.LogDebug("CreateUserAsync: Password hashed for user: {Email}; hash: {Hash}", email, hashedPassword);

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("CreateUserAsync: User created successfully: {User}", user);
            _logger.LogInformation("Exiting CreateUserAsync: email={Email}, userId={UserId}", email, user.Id);
            return user;
        }

        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public async Task<User> GetByEmailAsync(string email)
        {
            _logger.LogInformation("Entering GetByEmailAsync: email={Email}", email);
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email) ?? null!;

            if (user == null)
            {
                _logger.LogInformation("GetByEmailAsync: User not found with email: {Email}", email);
                _logger.LogInformation("Exiting GetByEmailAsync: email={Email}", email);
                return null!;
            }

            _logger.LogInformation("GetByEmailAsync: User found with email: {Email}", email);
            _logger.LogInformation("Exiting GetByEmailAsync: email={Email}, userId={UserId}", email, user.Id);
            return user;
        }

        /// <summary>
        /// Retrieves a user by their unique ID.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <returns>The user if found, otherwise null.</returns>
        public async Task<User> GetByIdAsync(int id)
        {
            _logger.LogInformation("Entering GetByIdAsync: userId={UserId}", id);
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogInformation("GetByIdAsync: User not found with ID: {Id}", id);
                _logger.LogInformation("Exiting GetByIdAsync: userId={UserId}", id);
                return null!;
            }

            _logger.LogInformation("GetByIdAsync: User found with ID: {Id}", id);
            _logger.LogInformation("Exiting GetByIdAsync: userId={UserId}", id);
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
            _logger.LogInformation("Entering VerifyPasswordHash: userId={UserId}", user?.Id);
            // Verify the password against the stored hash
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("VerifyPasswordHash: User or password hash is null or empty.");
                _logger.LogInformation("Exiting VerifyPasswordHash: userId={UserId}", user?.Id);
                return false;
            }

            var result = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
            _logger.LogInformation("Exiting VerifyPasswordHash: userId={UserId}, result={Result}", user.Id, result);
            return result;
        }

        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The user's new password.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ChangePasswordAsync(int userId, string oldPassword, string newPassword)
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
                throw new UnauthorizedAccessException("Old password is incorrect.");
            }

            // Hash the new password
            string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.PasswordHash = newHashedPassword;

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("ChangePasswordAsync: Password changed successfully for user ID: {UserId}", userId);
            _logger.LogInformation("Exiting ChangePasswordAsync: userId={UserId}", userId);
        }

        /// <summary>
        /// Updates the password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        public async Task<bool> UpdatePasswordAsync(int userId, string newPassword)
        {
            _logger.LogInformation("Entering UpdatePasswordAsync: userId={UserId}", userId);
            var user = await GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("UpdatePasswordAsync: User not found with ID: {UserId}", userId);
                _logger.LogInformation("Exiting UpdatePasswordAsync: userId={UserId}", userId);
                return false;
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("UpdatePasswordAsync: Password updated successfully for user ID: {UserId}", userId);
            _logger.LogInformation("Exiting UpdatePasswordAsync: userId={UserId}", userId);
            return true;
        }

        /// <summary>
        /// Updates the user DTO information.
        /// </summary>
        /// <param name="userDto">The user DTO to update.</param>
        /// <returns>The updated user DTO.</returns>
        public async Task<bool> UpdateUserDtoAsync(UserDto userDto)
        {
            _logger.LogInformation("Entering UpdateUserDtoAsync: userId={UserId}", userDto.Id);
            var user = await GetByIdAsync(userDto.Id);
            if (user == null)
            {
                _logger.LogWarning("UpdateUserDtoAsync: User not found with ID: {UserId}", userDto.Id);
                throw new ValidationException($"User not found with ID: {userDto.Id}");
            }

            user.Email = userDto.Email;
            var result = await _context.SaveChangesAsync() > 0;
            _logger.LogInformation("UpdateUserDtoAsync: User updated for userId={UserId}", userDto.Id);
            _logger.LogInformation("Exiting UpdateUserDtoAsync: userId={UserId}, result={Result}", userDto.Id, result);
            return result;
        }
    }
}