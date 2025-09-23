using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Backend.Model;
using System.Globalization;

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
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(GetConfigOrThrow<string>("Jwt:Key")));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: GetConfigOrThrow<string>("Jwt:Issuer"),
                audience: GetConfigOrThrow<string>("Jwt:Audience"),
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(GetConfigOrThrow<int>("Jwt:ExpireMinutes")),
                signingCredentials: creds
            );

            _logger.LogInformation("Generated JWT token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateAccessToken: userId={UserId}", user.Id);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshToken> GenerateRefreshTokenAsync(User user, string ipAddress, CancellationToken ct)
        {
            _logger.LogInformation("Entering GenerateRefreshTokenAsync: userId={UserId}, ipAddress={IpAddress}", user.Id, ipAddress);
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var refreshToken = new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(GetConfigOrThrow<int>("Jwt:RefreshExpireDays")),
                Created = DateTime.UtcNow,
                UserId = user.Id,
                CreatedByIp = ipAddress,
            };

            await _context.RefreshTokens.AddAsync(refreshToken, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Generated refresh token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateRefreshTokenAsync: userId={UserId}, ipAddress={IpAddress}", user.Id, ipAddress);
            return refreshToken;
        }

        public async Task<RefreshToken?> VerifyRefreshTokenAsync(string token, CancellationToken ct)
        {
            token = SanitizeInput(token);
            _logger.LogInformation("Entering VerifyRefreshTokenAsync: tokenStr={TokenStr}", token);
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

            // if the refresh token is not found or is expired/revoked, return false and revoke it
            // this is a security measure to prevent token reuse
            if (refreshToken == null || refreshToken.Expires < DateTime.UtcNow || refreshToken.IsRevoked)
            {
                if (refreshToken != null && refreshToken.Expires < DateTime.UtcNow && !refreshToken.IsRevoked)
                    await RevokeRefreshTokenAsync(token, ct);

                _logger.LogWarning("VerifyRefreshTokenAsync: Invalid or expired refresh token provided: {RefreshToken}", refreshToken);
                return null;
            }

            _logger.LogInformation("Exiting VerifyRefreshTokenAsync: tokenStr={TokenStr}", token);
            return refreshToken;
        }

        public async Task RevokeRefreshTokenAsync(string token, CancellationToken ct)
        {
            token = SanitizeInput(token);
            _logger.LogInformation("Entering RevokeRefreshTokenAsync");
            var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);
            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token could not be found.");
                return;
            }

            refreshToken.IsRevoked = true;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Revoked refresh token for user {UserId} at {Time}", refreshToken.UserId, DateTime.UtcNow);
            _logger.LogInformation("Exiting RevokeRefreshTokenAsync: userId={UserId}", refreshToken.UserId);
        }

        public string GenerateResetToken(User user)
        {
            _logger.LogInformation("Entering GenerateResetToken: userId={UserId}", user.Id);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetConfigOrThrow<string>("Jwt:Key"));

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString(CultureInfo.InvariantCulture)),
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
                Audience = GetConfigOrThrow<string>("Jwt:Audience"),
                Issuer = GetConfigOrThrow<string>("Jwt:Issuer")
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("Generated reset token for user {UserId} at {Time}", user.Id, DateTime.UtcNow);
            _logger.LogInformation("Exiting GenerateResetToken: userId={UserId}", user.Id);
            return tokenHandler.WriteToken(token);
        }

        public int? ValidateResetToken(string token)
        {
            token = SanitizeInput(token);
            _logger.LogInformation("Entering ValidateResetToken: token={Token}", token);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetConfigOrThrow<string>("Jwt:Key"));

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = GetConfigOrThrow<string>("Jwt:Issuer"),
                    ValidateAudience = true,
                    ValidAudience = GetConfigOrThrow<string>("Jwt:Audience"),
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

                var userId = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!, CultureInfo.InvariantCulture);
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

        private T GetConfigOrThrow<T>(string key)
        {
            var value = _config.GetValue<T>(key);
            if (value == null)
            {
                _logger.LogError("Missing required configuration value: {Key}", key);
                throw new InvalidOperationException($"Missing required configuration value: {key}");
            }

            return value;
        }

        private static string SanitizeInput(string input)
        {
            return input.Replace(Environment.NewLine, "").Trim();
        }
    }
}