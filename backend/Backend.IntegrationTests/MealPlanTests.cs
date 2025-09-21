using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Model;
using DotNet.Testcontainers.Builders;

namespace Backend.IntegrationTests
{
    public class MealPlanTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetMealPlans_Returns_MealPlans()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync("/api/mealplan?skip=0&take=10");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.MealPlans.Count());
            var mealPlans = result.MealPlans.ToList();
            mealPlans.ForEach(p => Assert.NotNull(p.Meals));

            // verify that we don't return the full object. We return meals, but not their contents
            var wasHit = false;
            mealPlans.ForEach(p =>
            {
                Assert.NotEqual(0, p.Id);
                p.Meals.ToList().ForEach(m =>
                {
                    Assert.NotEqual(0, m.Id);
                    if (m.RecipeId != null)
                    {
                        Assert.Null(m.Recipe);
                        wasHit = true;
                    }
                });
            });

            Assert.True(wasHit);
        }

        [Fact]
        public async Task GetMealPlans_Returns_401_withNoToken()
        {
            var response = await _client.GetAsync("/api/mealplan?skip=0&take=10");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetMealPlans_Returns_200_whenNoPlans()
        {
            await _factory.LoginAsync(_client);

            // delete the twop plans that are seeded
            var response = await _client.DeleteAsync("/api/mealplan/1");
            response.EnsureSuccessStatusCode();
            response = await _client.DeleteAsync("/api/mealplan/2");
            response.EnsureSuccessStatusCode();

            response = await _client.GetAsync("/api/mealplan");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.MealPlans);
        }
    }

    public class MealPlanPaginationTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanPaginationTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetMealPlans_Returns_pages_whenRequested()
        {
            await _factory.LoginAsync(_client);

            //seed 15 meals
            for (int i = 0; i < 15; i++)
            {
                var response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
                {
                    StartDate = DateTime.UtcNow,
                    Meals =
                    [
                        new CreateUpdateMealPlanEntryRequestDto
                        {
                             Notes = "note " + i,
                             RecipeId = 1
                        }
                    ]
                });

                response.EnsureSuccessStatusCode();
            }

            //ask for 5 at a time
            for (int i = 0; i < 21; i += 5)
            {
                var getResponse = await _client.GetAsync($"api/mealplan?skip={i}&take=5");

                getResponse.EnsureSuccessStatusCode();
                var result = await getResponse.Content.ReadFromJsonAsync<GetMealPlansResult>();
                Assert.NotNull(result);
                Assert.Equal(17, result.TotalCount);

                //first 3 times it should return 5 items. Last time it should return zero but not throw.
                if (i <= 10)
                    Assert.Equal(5, result.MealPlans.Count());
                else if (i < 16)
                    Assert.Equal(2, result.MealPlans.Count());
                else
                    Assert.Empty(result.MealPlans);
            }
        }
    }

    public class AddMealPlanTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public AddMealPlanTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AddMealPlans_Returns_DTO_onSuccess()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
            {
                StartDate = DateTime.UtcNow,
                Meals =
                    [
                        new CreateUpdateMealPlanEntryRequestDto
                        {
                             Notes = "new meal",
                             RecipeId = 1
                        }
                    ]
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MealPlanDto>();

            Assert.NotNull(result);
            Assert.NotEqual(0, result.Id);
            Assert.Single(result.Meals);

            // ensure we don't include full object, so we have recipe id but not the recipe
            Assert.Equal(1, result.Meals.First().RecipeId);
            Assert.Null(result.Meals.First().Recipe);
        }

        [Fact]
        public async Task AddMealPlans_Returns_BadRequest_onBadRecipe()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
            {
                Meals =
                    [
                        new CreateUpdateMealPlanEntryRequestDto
                        {
                             Notes = "new meal",
                             RecipeId = 10
                        }
                    ]
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddMealPlans_Returns_Unauthorized_withNoToken()
        {
            var response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
            {
                Meals =
                    [
                        new CreateUpdateMealPlanEntryRequestDto
                        {
                             Notes = "new meal",
                             RecipeId = 10
                        }
                    ]
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddMealPlans_Returns_BadRequest_withNoMeals()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
            {
                Meals = []
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    public class UpdateMealPlanTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public UpdateMealPlanTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task UpdateMealPlans_Returns_DTO_onSuccess()
        {
            await _factory.LoginAsync(_client);

            //add new recipe we can add to the meal plan
            var response = await _client.PostAsJsonAsync("api/recipe", new CreateUpdateRecipeDtoRequest
            {
                Source = "source",
                Title = "test title",
                Instructions = "stir",
                Ingredients = [new CreateUpdateRecipeIngredientDto
                { Quantity = 1, Unit = "bunch", Food = new NewFoodReferenceDto { CategoryId = 1, Name = "chicken" }}]
            });

            response.EnsureSuccessStatusCode();
            var recipeResult = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(recipeResult);
            var recipeId = recipeResult.Id;

            // grab the seeded mealplan with one meal.
            response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            var mealPlanDto = result.MealPlans.FirstOrDefault(m => m.Meals.Count() == 1);
            Assert.NotNull(mealPlanDto);

            // add a recipe to it
            var firstMeal = mealPlanDto.Meals.First();
            var mealPlanToUpdate = new CreateUpdateMealPlanRequestDto
            {
                Id = mealPlanDto.Id,
                StartDate = mealPlanDto.StartDate,
                Meals = [
                    new CreateUpdateMealPlanEntryRequestDto
                    {
                        Id = firstMeal.Id,
                        Notes = firstMeal.Notes,
                        RecipeId = recipeId
                    }
                ]
            };

            // send it
            response = await _client.PutAsJsonAsync($"api/mealplan/{mealPlanDto.Id}", mealPlanToUpdate);
            response.EnsureSuccessStatusCode();
            var newResult = await response.Content.ReadFromJsonAsync<MealPlanDto>();
            Assert.NotNull(newResult);
            Assert.Equal(mealPlanDto.Id, newResult.Id);
            var newId = newResult.Id;
            Assert.Equal(mealPlanDto.StartDate, newResult.StartDate);
            Assert.Equal(mealPlanDto.Meals.Count(), newResult.Meals.Count());
            Assert.Equal(firstMeal.Notes, newResult.Meals.First().Notes);
            Assert.Equal(recipeId, newResult.Meals.First().RecipeId);

            // verify it can be pulled back out with a GET
            response = await _client.GetAsync($"api/mealplan");
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            var mealToCheck = result.MealPlans.First(m => m.Id == newId);
            Assert.Equal(mealPlanDto.Id, newId);
            Assert.Equal(mealPlanDto.StartDate, mealToCheck.StartDate);
            Assert.Equal(mealPlanDto.Meals.Count(), mealToCheck.Meals.Count());
            Assert.Equal(firstMeal.Notes, mealToCheck.Meals.First().Notes);
            Assert.Equal(recipeId, mealToCheck.Meals.First().RecipeId);
        }

        [Fact]
        public async Task UpdateMealPlans_Returns_404_onNonExistentId()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PutAsJsonAsync($"api/mealplan/99", new CreateUpdateMealPlanRequestDto { Meals = [] });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
        
        [Fact]
        public async Task UpdateMealPlans_Returns_401_withNoToken()
        {
            var response = await _client.PutAsJsonAsync($"api/mealplan/99", new CreateUpdateMealPlanRequestDto{ Meals = [] });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}