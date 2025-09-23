using Backend.DTOs;

namespace Backend.Services
{
    public interface IUserService
    {
        /// <summary>
        /// Creates a new user with the specified email and password.
        /// </summary>
        /// <param name="request">The login request. Includes an email and password.</param>
        /// <param name="ip">The ip of the current request.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>Access and refresh tokens for the new user.</returns>
        Task<TokenResponse> RegisterNewUserAsync(LoginRequest request, string ip, CancellationToken ct);
        /// <summary>
        /// Logs in a user with the specified email and password.
        /// </summary>
        /// <param name="request">The login request including the email and password.</param>
        /// <param name="ip">The ip of the current requeset.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>Generated access and refresh tokens for th user.</returns>
        Task<TokenResponse> LoginAsync(LoginRequest request, string ip, CancellationToken ct);
        /// <summary>
        /// Logs out the user by revoking the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">Current refresh token for user to be revoked.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        Task LogoutAsync(string refreshToken, CancellationToken ct);
        /// <summary>
        /// Refreshes the authentication and refresh tokens using the provided refresh token.
        /// </summary>
        /// <param name="refreshToken">Long lasting token used to keep access open for user.</param>
        /// <param name="ip">The IP address of the current request.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>Token response containing new access and refresh token.</returns>
        Task<TokenResponse> RefreshTokensAsync(string refreshToken, string ip, CancellationToken ct);
        /// <summary>
        /// Updates the user DTO information. Email and any future user details can be updated.
        /// </summary>
        /// <param name="userDto">The user DTO to update.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>True on success.</returns>
        Task UpdateUserDtoAsync(UserDto userDto, CancellationToken ct);
        /// <summary>
        /// Changes the password for the specified user.
        /// </summary>
        /// <param name="userId">The user's unique identifier.</param>
        /// <param name="oldPassword">The user's current password.</param>
        /// <param name="newPassword">The user's new password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ChangePasswordAsync(int userId, string oldPassword, string newPassword, CancellationToken ct);
        /// <summary>
        /// Initiates the forgot password process for the specified email. Sends a reset email if the user exists.
        /// </summary>
        /// <param name="email">The email given by the user for resetting their forgotten password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        Task ForgotPasswordAsync(string email, CancellationToken ct);
        /// <summary>
        /// Updates the password for the specified user when they have forgotten their and cannot authenticate.
        /// </summary>
        /// <param name="request">The request to update the password including the reset code and the new password.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <returns>True if the update was successful, otherwise false.</returns>
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct);
    }
}
