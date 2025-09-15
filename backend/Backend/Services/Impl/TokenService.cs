using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;

namespace Backend.Services.Impl
{
    public class TokenService(IConfiguration config, PlannerContext context, ILogger<TokenService> logger) : ITokenService
    {
        private readonly IConfiguration _config = config;
        private readonly PlannerContext _context = context;
        private readonly ILogger<TokenService> _logger = logger;
        private readonly string _resetStr = "reset";

        public string GenerateAccessToken(User user)
        {
            _logger.LogInformation("Entering GenerateAccessToken: userId={UserId}", user.Id);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetConfigOrThrow("Jwt:Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: GetConfigOrThrow("Jwt:Issuer"),
                audience: GetConfigOrThrow("Jwt:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(GetConfigOrThrow("Jwt:ExpireMinutes"))),
                signingCredentials: creds
            );

            _logger.LogInformation("Generated JWT token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateAccessToken: userId={UserId}", user.Id);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string ipAddress)
        {
            _logger.LogInformation("Entering GenerateRefreshTokenAsync: userId={UserId}, ipAddress={IpAddress}", user.Id, ipAddress);
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(int.Parse(GetConfigOrThrow("Jwt:RefreshExpireDays"))),
                Created = DateTime.UtcNow,
                UserId = user.Id,
                CreatedByIp = ipAddress,
            };

            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Generated refresh token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateRefreshTokenAsync: userId={UserId}, ipAddress={IpAddress}", user.Id, ipAddress);
            return refreshToken;
        }

        public async Task<RefreshToken?> FindRefreshTokenAsync(string tokenStr)
        {
            _logger.LogInformation("Entering FindRefreshTokenAsync: tokenStr={TokenStr}", tokenStr);
            var token = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenStr);
            _logger.LogInformation("Found refresh token for user {UserId} at {Time}", token?.UserId, DateTime.UtcNow);
            _logger.LogInformation("Exiting FindRefreshTokenAsync: tokenStr={TokenStr}", tokenStr);
            return token;
        }

        public async Task RevokeRefreshTokenAsync(RefreshToken token)
        {
            _logger.LogInformation("Entering RevokeRefreshTokenAsync: userId={UserId}", token.UserId);
            token.IsRevoked = true;
            _context.RefreshTokens.Update(token);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Revoked refresh token for user {UserId} at {Time}", token.UserId, DateTime.UtcNow);
            _logger.LogInformation("Exiting RevokeRefreshTokenAsync: userId={UserId}", token.UserId);
        }

        public string GenerateResetToken(User user)
        {
            _logger.LogInformation("Entering GenerateResetToken: userId={UserId}", user.Id);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetConfigOrThrow("Jwt:Key"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(_resetStr, "true") // custom claim to mark as reset token
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1), // token valid for 1 hour
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature
                ),
                Audience = GetConfigOrThrow("Jwt:Audience"),
                Issuer = GetConfigOrThrow("Jwt:Issuer")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("Generated reset token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateResetToken: userId={UserId}", user.Id);
            return tokenHandler.WriteToken(token);
        }

        public int? ValidateResetToken(string token)
        {
            _logger.LogInformation("Entering ValidateResetToken: token={Token}", token);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetConfigOrThrow("Jwt:Key"));

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = GetConfigOrThrow("Jwt:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = GetConfigOrThrow("Jwt:Audience"),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                // Make sure it's actually a reset token
                var resetClaim = principal.FindFirst(_resetStr)?.Value;
                if (resetClaim != "true")
                {
                    _logger.LogWarning("ValidateResetToken: Invalid reset token: {Token}", token);
                    _logger.LogInformation("Exiting ValidateResetToken: token={Token}", token);
                    return null;
                }

                var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                int.TryParse(sub, out var userId);
                if (userId <= 0)
                {
                    _logger.LogWarning("ValidateResetToken: Invalid user ID in reset token: {Token}", token);
                    _logger.LogInformation("Exiting ValidateResetToken: token={Token}", token);
                    return null;
                }

                _logger.LogInformation("Exiting ValidateResetToken: token={Token}, userId={UserId}", token, userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ValidateResetToken: Error validating reset token: {Token}", token);
                _logger.LogInformation("Exiting ValidateResetToken: token={Token}", token);
                return null; // invalid or expired token
            }
        }

        private string GetConfigOrThrow(string key)
        {
            var value = _config[key];
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogError("Missing required configuration value: {Key}", key);
                throw new InvalidOperationException($"Missing required configuration value: {key}");
            }
            return value;
        }
    }
}