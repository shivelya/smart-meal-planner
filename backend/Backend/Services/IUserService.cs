using Backend.DTOs;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user by their email address.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <returns>The user if found, otherwise null.</returns>
        Task<User> GetByEmailAsync(string email);
        /// <summary>
        /// Retrieves a user by their unique ID.
        /// </summary>
        /// <param name="id">The user's unique identifier.</param>
        /// <returns>The user if found, otherwise null.</returns>
        Task<User> GetByIdAsync(int id);
        /// <summary>
        /// Creates a new user with the specified email and password.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="password">The user's password.</param>
        /// <returns>The created user.</returns>
        Task<User> CreateUserAsync(string email, string password);
        /// <summary>
        /// Updates the user DTO information.
        /// </summary>
        /// <param name="userDto">The user DTO to update.</param>
        /// <returns>True on success.</returns>
        Task<bool> UpdateUserDtoAsync(UserDto userDto);
        /// <summary>
        /// Verifies the password hash for the specified user.
        /// </summary>
        /// <param name="password">The password to verify.</param>
        /// <param name="user">The user whose password hash to verify.</param>
        /// <returns>True if the password matches, otherwise false.</returns>
        bool VerifyPasswordHash(string password, User user);
        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The user's new password.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ChangePasswordAsync(int userId, string oldPassword, string newPassword);
        /// <summary>
        /// Updates the password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="newPassword">The new password to set.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        Task<bool> UpdatePasswordAsync(int userId, string newPassword);
    }
}
