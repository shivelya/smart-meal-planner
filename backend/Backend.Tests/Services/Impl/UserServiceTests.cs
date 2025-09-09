using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Services.Impl
{
    public class UserServiceTests
    {
        private UserService CreateService()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();
            var userLogger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<UserService>();
            var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<PlannerContext>();
            var context = new PlannerContext(options, config, logger);
            return new UserService(context, userLogger);
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

        [Fact]
        public async Task ChangePasswordAsync_ChangesPassword_WhenOldPasswordIsCorrect()
        {
            var service = CreateService();
            var user = await service.CreateUserAsync("changepass@example.com", "oldpass");
            await service.ChangePasswordAsync(user.Id.ToString(), "oldpass", "newpass");

            var updatedUser = await service.GetByIdAsync(user.Id);
            Assert.True(service.VerifyPasswordHash("newpass", updatedUser));
            Assert.False(service.VerifyPasswordHash("oldpass", updatedUser));
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenOldPasswordIsIncorrect()
        {
            var service = CreateService();
            var user = await service.CreateUserAsync("wrongold@example.com", "oldpass");
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                service.ChangePasswordAsync(user.Id.ToString(), "wrongpass", "newpass"));
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenUserNotFound()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                service.ChangePasswordAsync("9999", "oldpass", "newpass"));
        }

        [Fact]
        public async Task ChangePasswordAsync_Throws_WhenInvalidUserGiven()
        {
            var service = CreateService();
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ChangePasswordAsync("", "oldpass", "newpass"));

            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.ChangePasswordAsync("not an id", "oldpass", "newpass"));
        }

        [Fact]
        public async Task UpdatePasswordAsync_UpdatesPassword_WhenUserExists()
        {
            var service = CreateService();
            var user = await service.CreateUserAsync("updatepass@example.com", "oldpass");
            var result = await service.UpdatePasswordAsync(user.Id, "newpass");

            Assert.True(result);
            var updatedUser = await service.GetByIdAsync(user.Id);
            Assert.True(service.VerifyPasswordHash("newpass", updatedUser));
        }

        [Fact]
        public async Task UpdatePasswordAsync_ReturnsFalse_WhenUserNotFound()
        {
            var service = CreateService();
            var result = await service.UpdatePasswordAsync(9999, "newpass");
            Assert.False(result);
        }
    }
}
