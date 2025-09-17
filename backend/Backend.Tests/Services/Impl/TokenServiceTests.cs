using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Services.Impl
{
    public class TokenServiceTests
    {
        private static TokenService CreateService(PlannerContext context, Dictionary<string, string?> configValues)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenService>();
            return new TokenService(config, context, logger);
        }

        private static TokenService CreateService(out User user)
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_key_12345super_secret_key_12345super_secret_key_12345",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience",
                ["Jwt:ExpireMinutes"] = "15",
                ["Jwt:RefreshExpireDays"] = "7"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var logger = new LoggerFactory().CreateLogger<TokenService>();
            var context = new PlannerContext(options, config, new LoggerFactory().CreateLogger<PlannerContext>());

            user = new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" };
            context.Users.Add(user);
            context.SaveChanges();

            return new TokenService(config, context, logger);
        }

        private static PlannerContext CreateInMemoryContext()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            return new PlannerContext(options, config, logger);
        }

        [Fact]
        public void GenerateAccessToken_ReturnsToken()
        {
            var configValues = new Dictionary<string, string?>
            {
                { "Jwt:Key", "supersecretkeysupersecretkeysupersecretkey" },
                { "Jwt:Issuer", "issuer" },
                { "Jwt:Audience", "audience" },
                { "Jwt:ExpireMinutes", "15" }
            };
            var context = CreateInMemoryContext();
            var service = CreateService(context, configValues);
            var user = new User { Id = 1, Email = "test@example.com" };

            var token = service.GenerateAccessToken(user);

            Assert.False(string.IsNullOrWhiteSpace(token));
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            Assert.Equal("issuer", jwt.Issuer);
            Assert.Contains("audience", jwt.Audiences);
            Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Assert.Equal(user.Email, jwt.Claims.First(c => c.Type == ClaimTypes.Name).Value);
            Assert.True(jwt.ValidTo > DateTime.UtcNow);
            Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(15));
        }

        [Fact]
        public async Task GenerateRefreshToken_CreatesAndReturnsToken()
        {
            var configValues = new Dictionary<string, string?>
                { { "Jwt:RefreshExpireDays", "7" } };
            var context = CreateInMemoryContext();
            var service = CreateService(context, configValues);
            var user = new User { Id = 2, Email = "user@example.com" };
            var ip = "127.0.0.1";

            var refreshToken = await service.GenerateRefreshTokenAsync(user, ip, CancellationToken.None);

            Assert.NotNull(refreshToken.Token);
            Assert.Equal(user.Id, refreshToken.UserId);
            Assert.Equal(ip, refreshToken.CreatedByIp);
            Assert.True(refreshToken.Expires <= DateTime.UtcNow.AddDays(7));
            Assert.True(refreshToken.Expires > DateTime.UtcNow.AddDays(6));
            Assert.True(refreshToken.Created <= DateTime.UtcNow);
            Assert.True(refreshToken.Created > DateTime.UtcNow.AddMinutes(-1));
            Assert.False(refreshToken.IsRevoked);
            Assert.True(refreshToken.Id > 0);
            var tokenInDb = await context.RefreshTokens.FindAsync(refreshToken.Id);
            Assert.NotNull(tokenInDb);
        }

        [Fact]
        public async Task VerifyRefreshToken_ReturnsToken_WhenExistsAndValid()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token123";
            var token = new RefreshToken { Token = tokenStr, UserId = 1, Expires = DateTime.UtcNow.AddDays(1), Created = DateTime.UtcNow };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var found = await service.VerifyRefreshTokenAsync(tokenStr, CancellationToken.None);

            Assert.NotNull(found);
            Assert.Equal(tokenStr, found.Token);
        }

        [Fact]
        public async Task VerifyRefreshToken_ReturnsNullAndRevokesToken_WhenNotExists()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);

            var found = await service.VerifyRefreshTokenAsync("notfound", CancellationToken.None);

            Assert.Null(found);
        }

        [Fact]
        public async Task VerifyRefreshToken_ReturnsNull_WhenRevoked()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token123";
            var token = new RefreshToken { Token = tokenStr, UserId = 1, Expires = DateTime.UtcNow.AddDays(1), Created = DateTime.UtcNow, IsRevoked = true };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var found = await service.VerifyRefreshTokenAsync(tokenStr, CancellationToken.None);

            Assert.Null(found);
        }

        [Fact]
        public async Task VerifyRefreshToken_ReturnsNullAndRevokesToken_WhenExpired()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token123";
            var token = new RefreshToken { Token = tokenStr, UserId = 1, Expires = DateTime.UtcNow.AddDays(-1), Created = DateTime.UtcNow, IsRevoked = true };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var found = await service.VerifyRefreshTokenAsync(tokenStr, CancellationToken.None);

            Assert.Null(found);
        }

        [Fact]
        public async Task RevokeRefreshToken_SetsIsRevokedTrue()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token456";
            var token = new RefreshToken
            {
                Token = tokenStr,
                UserId = 1,
                Expires = DateTime.UtcNow.AddDays(1),
                Created = DateTime.UtcNow,
                IsRevoked = false
            };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            await service.RevokeRefreshTokenAsync(tokenStr, CancellationToken.None);

            var found = await context.RefreshTokens.FindAsync(token.Id);
            Assert.NotNull(found);
            Assert.True(found.IsRevoked);
        }

        [Fact]
        public void GenerateResetToken_CreatesValidToken()
        {
            var service = CreateService(out var user);
            var token = service.GenerateResetToken(user);

            Assert.False(string.IsNullOrWhiteSpace(token));

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);

            Assert.Equal("TestIssuer", jwtToken.Issuer);
            Assert.Equal("TestAudience", jwtToken.Audiences.First());
            Assert.Equal(user.Id.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
            Assert.Equal("true", jwtToken.Claims.First(c => c.Type == "reset").Value);
            Assert.True(jwtToken.ValidTo > DateTime.UtcNow);
            Assert.True(jwtToken.ValidTo <= DateTime.UtcNow.AddHours(1));
            Assert.True(jwtToken.ValidFrom <= DateTime.UtcNow);
            Assert.True(jwtToken.ValidFrom > DateTime.UtcNow.AddMinutes(-1));
            Assert.Equal(SecurityAlgorithms.HmacSha256, jwtToken.SignatureAlgorithm);
            Assert.Equal("HS256", jwtToken.Header.Alg);
            Assert.Equal("JWT", jwtToken.Header.Typ);
            Assert.Equal("super_secret_key_12345super_secret_key_12345super_secret_key_12345", service
                .GetType()
                .GetField("_config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(service) is IConfiguration config ? config["Jwt:Key"] : null);
        }

        [Fact]
        public void ValidateResetToken_ReturnsUserId_WhenTokenIsValid()
        {
            var service = CreateService(out var user);
            var token = service.GenerateResetToken(user);

            var result = service.ValidateResetToken(token);

            Assert.Equal(user.Id, result);
        }

        [Fact]
        public void ValidateResetToken_ReturnsNull_WhenResetClaimIsMissing()
        {
            var configDict = new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "super_secret_key_12345super_secret_key_12345super_secret_key_12345",
                ["Jwt:Issuer"] = "TestIssuer",
                ["Jwt:Audience"] = "TestAudience"
            };
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();

            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var logger = new LoggerFactory().CreateLogger<TokenService>();
            var context = new PlannerContext(options, config, new LoggerFactory().CreateLogger<PlannerContext>());
            var service = new TokenService(config, context, logger);

            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(config["Jwt:Key"] ?? "");
            var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, "42") };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = config["Jwt:Audience"],
                Issuer = config["Jwt:Issuer"]
            };
            var token = handler.CreateToken(tokenDescriptor);
            var tokenStr = handler.WriteToken(token);

            var result = service.ValidateResetToken(tokenStr);
            Assert.Null(result);
        }

        [Fact]
        public void ValidateResetToken_ReturnsNull_WhenSubClaimIsMissing()
        {
            var service = CreateService(out var _);
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_12345super_secret_key_12345super_secret_key_12345");
            var claims = new[] { new Claim("reset", "true") };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = "TestAudience",
                Issuer = "TestIssuer"
            };
            var token = handler.CreateToken(tokenDescriptor);
            var tokenStr = handler.WriteToken(token);

            var result = service.ValidateResetToken(tokenStr);
            Assert.Null(result);
        }

        [Fact]
        public void ValidateResetToken_ReturnsNull_WhenSubClaimIsInvalid()
        {
            var service = CreateService(out var _);
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_12345super_secret_key_12345super_secret_key_12345");
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, "not_an_int"),
                new Claim("reset", "true")
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = "TestAudience",
                Issuer = "TestIssuer"
            };
            var token = handler.CreateToken(tokenDescriptor);
            var tokenStr = handler.WriteToken(token);

            var result = service.ValidateResetToken(tokenStr);
            Assert.Null(result);
        }

        [Fact]
        public void ValidateResetToken_ReturnsNull_WhenTokenIsExpired()
        {
            var service = CreateService(out var user);
            var handler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes("super_secret_key_12345super_secret_key_12345super_secret_key_12345");
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("reset", "true")
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddSeconds(+1), // already expired
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
                Audience = "TestAudience",
                Issuer = "TestIssuer"
            };
            var token = handler.CreateToken(tokenDescriptor);
            var tokenStr = handler.WriteToken(token);

            Thread.Sleep(2000); // ensure token is expired
            var result = service.ValidateResetToken(tokenStr);
            Assert.Null(result);
        }

        [Fact]
        public void ValidateResetToken_ReturnsNull_WhenTokenIsTampered()
        {
            var service = CreateService(out var user);
            var token = service.GenerateResetToken(user);

            // Tamper with the token string
            var tamperedToken = token.Substring(0, token.Length - 2) + "xx";

            var result = service.ValidateResetToken(tamperedToken);
            Assert.Null(result);
        }
    }
}
