using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    public class AccountTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public AccountTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Register_Returns_token()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/register", new LoginRequest
            {
                Email = "test@example.com",
                Password = "testpass"
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<TokenResponse>();

            Assert.NotNull(result?.AccessToken);
            Assert.NotNull(result?.RefreshToken);
        }
    }
}