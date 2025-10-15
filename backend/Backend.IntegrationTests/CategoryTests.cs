using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class CategoryTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public CategoryTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetCategories_ReturnsAllCategories()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/category");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetCategoriesResult>();
            Assert.NotNull(result);
            Assert.True(result.TotalCount >= 5); // assuming at least 5 seeded categories
            Assert.NotEmpty(result.Items);
        }

        [Fact]
        public async Task GetCategories_Unauthorized_WithoutLogin()
        {
            var response = await _client.GetAsync("/api/category");
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}