using Backend.Model;

namespace Backend.Services
{
    /// <summary>
    /// Interface for JWT token generation.
    /// </summary>
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        Task<RefreshToken> GenerateRefreshToken(User user, string ipAddress);
        Task<RefreshToken?> FindRefreshToken(string token);
        Task RevokeRefreshToken(RefreshToken token);
        string GenerateResetToken(User user);
        int? ValidateResetToken(string token);
    }
}