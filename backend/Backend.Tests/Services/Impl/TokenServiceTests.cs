using Microsoft.Extensions.Configuration;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Services.Impl
{
    public class TokenServiceTests
    {
        private TokenService CreateService(PlannerContext context, Dictionary<string, string?> configValues)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenService>();
            return new TokenService(config, context, logger);
        }

        private PlannerContext CreateInMemoryContext()
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

            var refreshToken = await service.GenerateRefreshToken(user, ip);

            Assert.NotNull(refreshToken.Token);
            Assert.Equal(user.Id, refreshToken.UserId);
            Assert.Equal(ip, refreshToken.CreatedByIp);
            Assert.True(refreshToken.Expires > DateTime.UtcNow);
        }

        [Fact]
        public async Task FindRefreshToken_ReturnsToken_WhenExists()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token123";
            var token = new RefreshToken { Token = tokenStr, UserId = 1, Expires = DateTime.UtcNow.AddDays(1), Created = DateTime.UtcNow };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();

            var found = await service.FindRefreshToken(tokenStr);

            Assert.NotNull(found);
            Assert.Equal(tokenStr, found.Token);
        }

        [Fact]
        public async Task FindRefreshToken_ReturnsNull_WhenNotExists()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);

            var found = await service.FindRefreshToken("notfound");

            Assert.Null(found);
        }

        [Fact]
        public async Task RevokeRefreshToken_SetsIsRevokedTrue()
        {
            var context = CreateInMemoryContext();
            var configValues = new Dictionary<string, string?>();
            var service = CreateService(context, configValues);
            var tokenStr = "token456";
            var token = new RefreshToken { Token = tokenStr, UserId = 1, Expires = DateTime.UtcNow.AddDays(1), Created = DateTime.UtcNow, IsRevoked = false };
            context.RefreshTokens.Add(token);
            await context.SaveChangesAsync();
            await service.RevokeRefreshToken(token);

            var found = await service.FindRefreshToken(tokenStr);

            Assert.NotNull(found);
            Assert.True(found.IsRevoked);
        }
    }
}
