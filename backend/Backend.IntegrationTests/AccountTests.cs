using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    public class AccountTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AccountTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_Returns_token()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new LoginRequest
            {
                Email = "test2@example.com",
                Password = "testpass"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            Assert.NotNull(result?.AccessToken);
            Assert.NotNull(result?.RefreshToken);
        }

        [Fact]
        public async Task Register_NoPassword_Returns_400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                Email = "test2@example.com",
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_NoEmail_Returns_400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new
            {
                Password = "password",
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_BadEmail_Returns_400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new LoginRequest
            {
                Email = "bademail",
                Password = "password"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Register_DuplicateEmail_Returns_409()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new LoginRequest
            {
                Email = "test@example.com",
                Password = "password"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task Login_Returns_token()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = "test@example.com",
                Password = "password"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            Assert.NotNull(result?.AccessToken);
            Assert.NotNull(result?.RefreshToken);
        }

        [Fact]
        public async Task Login_BadPassword_Returns_401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = "test@example.com",
                Password = "wrong"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_BadEmail_Returns_401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest
            {
                Email = "wrong@example.com",
                Password = "password"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Login_MissingEmail_Returns_400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login", new
            {
                Password = "password"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Logout_Returns_200_evenIfRefreshTokenBad()
        {
            var tokens = await _factory.Login(_client);
            var response = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest
            {
                RefreshToken = tokens.RefreshToken
            });

            response.EnsureSuccessStatusCode();

            var newResponse = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest
            {
                RefreshToken = tokens.RefreshToken
            });

            newResponse.EnsureSuccessStatusCode();
        }
    }
}