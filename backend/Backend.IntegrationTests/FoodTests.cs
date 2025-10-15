using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class FoodTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public FoodTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetFood_ReturnsFood_WithPagination()
        {
            await _factory.LoginAsync(_client);

            // seed 15 pantry items with new foods
            for (int i = 1; i <= 15; i++)
            {
                var food = new CreateUpdatePantryItemRequestDto
                {
                    Food = new NewFoodReferenceDto { Name = $"Food {i}", CategoryId = 1 },
                    Quantity = 1,
                    Unit = "pcs"
                };

                var response = await _client.PostAsJsonAsync("/api/pantryitem", food);
                response.EnsureSuccessStatusCode();
            }

            // fetch foods with skip and take
            var take = 5;
            var total = 23;
            for (int i = 0; i < 23; i += take)
            {
                var response = await _client.GetAsync($"/api/food?skip={i}&take={take}");
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
                Assert.NotNull(result);
                Assert.Equal(total, result.TotalCount); // 15 plus 8 existing seeded foods

                if (i + take <= total)
                    Assert.Equal(take, result.Items.Count());
                else
                    Assert.Equal(total - i, result.Items.Count());
            }
        }

        [Fact]
        public async Task GetFood_ReturnsFood_BasedOnQuery()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync("/api/food?query=Milk");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetFood_ReturnsFood_WithPartialMatch()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync("/api/food?query=fried");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(result);
            Assert.Equal(1, result.TotalCount);
        }

        [Fact]
        public async Task GetFood_ReturnsBadRequest_WithNegativeSkip()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync("/api/food?query=fried&skip=-5");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFood_ReturnsBadRequest_WithNegativeTake()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync("/api/food?query=fried&take=-5");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFood_ReturnsUnauthorized_IfNotLoggedIn()
        {
            var response = await _client.GetAsync("/api/food?query=fried");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}