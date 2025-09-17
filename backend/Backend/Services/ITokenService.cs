using Backend.Model;

namespace Backend.Services
{
    /// <summary>
    /// Interface for JWT token generation.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Generates a JWT access token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom to generate the access token.</param>
        /// <returns>The generated JWT access token as a string.</returns>
        string GenerateAccessToken(User user);
        /// <summary>
        /// Generates a refresh token for the specified user and IP address.
        /// </summary>
        /// <param name="user">The user for whom to generate the refresh token.</param>
        /// <param name="ipAddress">The IP address associated with the refresh token.</param>
        /// <param name="ct">The cancellation token for the action.</param>
        /// <returns>The generated refresh token.</returns>
        Task<RefreshToken> GenerateRefreshTokenAsync(User user, string ipAddress, CancellationToken ct);
        /// <summary>
        /// Finds a refresh token by its token string.
        /// </summary>
        /// <param name="token">The refresh token string to search for.</param>
        /// <param name="ct">The cancellation token for the action.</param>
        /// <returns>The found refresh token if it exists, otherwise null.</returns>
        Task<RefreshToken?> VerifyRefreshTokenAsync(string token, CancellationToken ct);
        /// <summary>
        /// Revokes the specified refresh token.
        /// </summary>
        /// <param name="token">The refresh token to revoke.</param>
        /// <param name="ct">The cancellation token for the action.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task RevokeRefreshTokenAsync(string token, CancellationToken ct);
        /// <summary>
        /// Generates a password reset token for the specified user.
        /// </summary>
        /// <param name="user">The user for whom to generate the reset token.</param>
        /// <returns>The generated reset token as a string.</returns>
        string GenerateResetToken(User user);
        /// <summary>
        /// Validates a password reset token and returns the user ID if valid.
        /// </summary>
        /// <param name="token">The reset token to validate.</param>
        /// <returns>The user ID if the token is valid, otherwise null.</returns>
        int? ValidateResetToken(string token);
    }
}