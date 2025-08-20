using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Services.Impl
{
    public class UserServiceTests
    {
        private UserSerivce CreateService()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var userLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserSerivce>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            return new UserSerivce(context, userLogger);
        }

        [Fact]
        public async Task CreateUserAsync_CreatesUserWithHashedPassword()
        {
            var service = CreateService();
            var email = "test@example.com";
            var password = "password123";

            var user = await service.CreateUserAsync(email, password);

            Assert.Equal(email, user.Email);
            Assert.NotNull(user.PasswordHash);
            Assert.NotEqual(password, user.PasswordHash);
            Assert.True(service.VerifyPasswordHash(password, user));
        }

        [Fact]
        public async Task CreateUserAsync_Throws_WhenUserExists()
        {
            var service = CreateService();
            var email = "test@example.com";

            await service.CreateUserAsync(email, "password123");

            await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateUserAsync(email, "password456"));
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsUser_WhenExists()
        {
            var service = CreateService();
            var email = "test@example.com";
            var created = await service.CreateUserAsync(email, "password123");

            var user = await service.GetByEmailAsync(email);

            Assert.NotNull(user);
            Assert.Equal(created.Id, user.Id);
        }

        [Fact]
        public async Task GetByEmailAsync_ReturnsNull_WhenNotExists()
        {
            var service = CreateService();

            var user = await service.GetByEmailAsync("notfound@example.com");

            Assert.Null(user);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsUser_WhenExists()
        {
            var service = CreateService();
            var created = await service.CreateUserAsync("test@example.com", "password123");

            var user = await service.GetByIdAsync(created.Id);

            Assert.NotNull(user);
            Assert.Equal(created.Email, user.Email);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotExists()
        {
            var service = CreateService();

            var user = await service.GetByIdAsync(999);

            Assert.Null(user);
        }

        [Fact]
        public void VerifyPasswordHash_ReturnsFalse_WhenUserIsNull()
        {
            var service = CreateService();

            var result = service.VerifyPasswordHash("password", null!);

            Assert.False(result);
        }

        [Fact]
        public void VerifyPasswordHash_ReturnsFalse_WhenPasswordHashIsNullOrEmpty()
        {
            var service = CreateService();
            var user = new User { Email = "test@example.com", PasswordHash = null! };

            Assert.False(service.VerifyPasswordHash("password", user));

            user.PasswordHash = "";

            Assert.False(service.VerifyPasswordHash("password", user));
        }

        [Fact]
        public void VerifyPasswordHash_ReturnsTrue_WhenPasswordIsCorrect()
        {
            var service = CreateService();
            var password = "password123";
            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            var user = new User { Email = "test@example.com", PasswordHash = hash };

            Assert.True(service.VerifyPasswordHash(password, user));
        }

        [Fact]
        public void VerifyPasswordHash_ReturnsFalse_WhenPasswordIsIncorrect()
        {
            var service = CreateService();
            var hash = BCrypt.Net.BCrypt.HashPassword("password123");
            var user = new User { Email = "test@example.com", PasswordHash = hash };

            Assert.False(service.VerifyPasswordHash("wrongpassword", user));
        }
    }
}
