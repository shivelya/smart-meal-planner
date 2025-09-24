using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class AccountTests
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
            var tokens = await _factory.LoginAsync(_client);
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

        [Fact]
        public async Task Logout_Returns_400_missingToken()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.PostAsJsonAsync("/api/auth/logout", new { });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Logout_Returns_400_badToken()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest { RefreshToken = "" });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Returns_200_withValidToken()
        {
            var tokens = await _factory.LoginAsync(_client);
            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
            {
                RefreshToken = tokens.RefreshToken
            });

            response.EnsureSuccessStatusCode();
            var newTokens = await response.Content.ReadFromJsonAsync<TokenResponse>();

            Assert.NotNull(newTokens);
            Assert.NotNull(newTokens.AccessToken);
            Assert.NotNull(newTokens.RefreshToken);

            // use token to hit protected route
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newTokens.AccessToken);
            var newResponse = await _client.GetAsync("/api/pantryItem");

            newResponse.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task Refresh_Returns_401_withInvalidToken()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
            {
                RefreshToken = "invalid token"
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Refresh_Returns_401_withExpiredToken()
        {
            var factory = new CustomWebApplicationFactory();
            factory.ConfigValues = new Dictionary<string, string> { { "Jwt:RefreshExpireDays", "-1" } };
            await factory.InitializeAsync();
            var client = factory.CreateClient();

            var tokens = await factory.LoginAsync(client);
            var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest
            {
                RefreshToken = tokens.RefreshToken
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}