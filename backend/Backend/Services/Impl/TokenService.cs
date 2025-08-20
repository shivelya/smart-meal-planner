using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;

namespace Backend.Services.Impl
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _config;
        private readonly PlannerContext _context;
        private readonly ILogger<TokenService> _logger;
        public TokenService(IConfiguration config, PlannerContext context, ILogger<TokenService> logger)
        {
            _config = config;
            _context = context;
            _logger = logger;
        }

        public string GenerateAccessToken(User user)
        {
            var claims = new[]
            {
                new Claim("id", user.Id.ToString()),
                new Claim("email", user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"] ?? ""));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "15")),
                signingCredentials: creds
            );

            _logger.LogInformation("Generated JWT token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshToken(User user, string ipAddress)
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshExpireDays"] ?? "7")),
                Created = DateTime.UtcNow,
                UserId = user.Id,
                CreatedByIp = ipAddress,
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated refresh token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);

            return refreshToken;
        }

        public async Task<RefreshToken?> FindRefreshToken(string tokenStr)
        {
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenStr);
            _logger.LogInformation("Found refresh token for user {UserId} at {Time}", token?.UserId, DateTime.UtcNow);
            return token;
        }

        public async Task RevokeRefreshToken(RefreshToken token)
        {
            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked refresh token for user {UserId} at {Time}", token.UserId, DateTime.UtcNow);
        }
    }
}