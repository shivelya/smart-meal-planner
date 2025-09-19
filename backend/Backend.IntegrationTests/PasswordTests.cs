using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests
{
    public class PasswordTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public PasswordTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task ChangePassword_200_WithCorrectOldPassword()
        {
            await _factory.Login(_client);

            var result = await _client.PutAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
            {
                OldPassword = "password",
                NewPassword = "newpass"
            });

            result.EnsureSuccessStatusCode();

            result = await _client.PutAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
            {
                OldPassword = "newpass",
                NewPassword = "password"
            });

            result.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ChangePassword_400_WithWrongOldPassword()
        {
            await _factory.Login(_client);

            var result = await _client.PutAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
            {
                OldPassword = "wrong",
                NewPassword = "newpass"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task ChangePassword_400_WithMissingData()
        {
            await _factory.Login(_client);

            var result = await _client.PutAsJsonAsync("api/auth/change-password", new
            {
                OldPassword = "wrong"
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);

            result = await _client.PutAsJsonAsync("api/auth/change-password", new
            {
                NewPassword = "newpass"
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ForgotPassword_SendEmail_WithRegisteredEmail()
        {
            var result = await _client.PostAsJsonAsync("api/auth/forgot-password", new DTOs.ForgotPasswordRequest
            {
                Email = "test@example.com"
            });

            result.EnsureSuccessStatusCode();

            //verify email sent
            using var scope = _factory.Services.CreateScope();
            var fakeEmail = scope.ServiceProvider.GetRequiredService<FakeEmailService>();

            Assert.NotEmpty(fakeEmail.Sent);

            var sent = fakeEmail.Sent.Last();
            Assert.Equal("test@example.com", sent.To);
            Assert.NotNull(sent.ResetCode);


            // go ahead and reset password
            result = await _client.PostAsJsonAsync("api/auth/reset-password", new DTOs.ResetPasswordRequest
            {
                ResetCode = sent.ResetCode,
                NewPassword = "newpass"
            });

            result.EnsureSuccessStatusCode();

            // can log in with new password
            result = await _client.PostAsJsonAsync("api/auth/login", new DTOs.LoginRequest
            {
                Email = "test@example.com",
                Password = "newpass"
            });

            result.EnsureSuccessStatusCode();
            var loginResponse = await result.Content.ReadFromJsonAsync<TokenResponse>();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse!.AccessToken);

            //put the password back for other tests
            result = await _client.PutAsJsonAsync("api/auth/change-password", new ChangePasswordRequest
            {
                OldPassword = "newpass",
                NewPassword = "password"
            });

            result.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ForgotPassword_200_WithBadEmail()
        {
            var result = await _client.PostAsJsonAsync("api/auth/forgot-password", new DTOs.ForgotPasswordRequest
            {
                Email = "bad@example.com"
            });

            result.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task ResetPassword_400_WithInvalidToken()
        {
            var result = await _client.PostAsJsonAsync("api/auth/reset-password", new DTOs.ResetPasswordRequest
            {
                ResetCode = "badtoken",
                NewPassword = "newpass"
            });

            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }
    }

    public record SentEmail(string To, string ResetCode, DateTimeOffset SentAt);

    public class FakeEmailService : IEmailService
    {
        private readonly List<SentEmail> _sent = new();

        // Thread-safe read access
        public IReadOnlyList<SentEmail> Sent => _sent.AsReadOnly();

        public Task SendPasswordResetEmailAsync(string to, string resetCode)
        {
            lock (_sent) // keep it safe for parallel test runs (if any)
            {
                _sent.Add(new SentEmail(to, resetCode, DateTimeOffset.UtcNow));
            }
            return Task.CompletedTask;
        }

        // Helper to clear captured emails between tests
        public void Clear() 
        {
            lock (_sent) { _sent.Clear(); }
        }
    }
}