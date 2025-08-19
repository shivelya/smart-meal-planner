using Xunit;
using System;
using Backend.Model;

namespace Backend.Tests.Model
{
    public class RefreshTokenTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var now = DateTime.UtcNow;
            var token = new RefreshToken
            {
                Id = 1,
                Token = "abc123",
                UserId = 42,
                Expires = now.AddDays(7),
                IsRevoked = false,
                Created = now,
                CreatedByIp = "127.0.0.1"
            };
            Assert.Equal(1, token.Id);
            Assert.Equal("abc123", token.Token);
            Assert.Equal(42, token.UserId);
            Assert.Equal(now.AddDays(7), token.Expires);
            Assert.False(token.IsRevoked);
            Assert.Equal(now, token.Created);
            Assert.Equal("127.0.0.1", token.CreatedByIp);
        }

        [Fact]
        public void CanRevokeToken()
        {
            var token = new RefreshToken { IsRevoked = false };
            token.IsRevoked = true;
            Assert.True(token.IsRevoked);
        }

        [Fact]
        public void TokenExpiresCorrectly()
        {
            var token = new RefreshToken { Expires = DateTime.UtcNow.AddMinutes(5) };
            Assert.True(token.Expires > DateTime.UtcNow);
        }
    }
}
