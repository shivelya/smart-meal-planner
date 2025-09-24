using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;

namespace Backend.IntegrationTests
{
    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<CustomWebApplicationFactory>
    {
        // This class is just a marker; it doesn’t contain any code
    }

    [Collection("Database collection")]
    public class MealPlanGetTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanGetTests(CustomWebApplicationFactory factory)
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

    [Collection("Database collection")]
    public class MealPlanPaginationTests
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
            for (int i = 0; i < 17; i++)
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

    [Collection("Database collection")]
    public class MealPlanAddTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanAddTests(CustomWebApplicationFactory factory)
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

    [Collection("Database collection")]
    public class MealPlanUpdateTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanUpdateTests(CustomWebApplicationFactory factory)
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
            var response = await _client.PutAsJsonAsync($"api/mealplan/99", new CreateUpdateMealPlanRequestDto { Meals = [] });
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Collection("Database collection")]
    public class MealPlanDeleteTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanDeleteTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task DeleteMealPlans_Returns_NoContent_onSuccess()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            var id = result.MealPlans.First().Id;

            response = await _client.DeleteAsync($"api/mealplan/{id}");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // verify we can no longer find that meal plan
            response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            Assert.DoesNotContain(id, result.MealPlans.Select(m => m.Id));
        }

        [Fact]
        public async Task DeleteMealPlans_Returns_notFound_withBadId()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.DeleteAsync("api/mealplan/99");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteMealPlans_Returns_Unauthorized_withNoToken()
        {
            var response = await _client.DeleteAsync("api/mealplan/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Collection("Database collection")]
    public class MealPlanGenerateTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanGenerateTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GenerateMealPlans_Returns_Created_onSuccess()
        {
            await _factory.LoginAsync(_client);

            var startDate = DateTime.UtcNow;
            var response = await _client.PostAsJsonAsync("api/mealplan/generate", new GenerateMealPlanRequestDto
            {
                Days = 1,
                StartDate = startDate
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CreateUpdateMealPlanRequestDto>();
            Assert.NotNull(result);
            Assert.Equal(startDate, result.StartDate);
            Assert.Single(result.Meals);

            Assert.Null(result.Id); // because it doesn't get saved to DB til user persists it
        }

        [Fact]
        public async Task GenerateMealPlans_Returns_Unauthorized_onNoToken()
        {
            var response = await _client.PostAsJsonAsync("api/mealplan/generate", new GenerateMealPlanRequestDto
            {
                Days = 1
            });

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GenerateMealPlans_Returns_maxConfiguredMeals()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/mealplan/generate", new GenerateMealPlanRequestDto
            {
                Days = 100,
                StartDate = DateTime.UtcNow
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GenerateMealPlans_Uses_ExternalSource_WhenRequested()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.PostAsJsonAsync("api/mealplan/generate", new GenerateMealPlanRequestDto
            {
                UseExternal = true,
                Days = 10
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<CreateUpdateMealPlanRequestDto>();
            Assert.NotNull(result);
            foreach (var meal in result.Meals.Cast<GeneratedMealPlanEntryDto>())
            {
                Assert.Contains("Spoonacular", meal.Source);
            }
        }
    }

    [Collection("Database collection")]
    public class MealPlanCookTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanCookTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task CookMealPlans_Returns_ListofItems_onSuccess()
        {
            await _factory.LoginAsync(_client);

            // quantities shouldn't change before and after cooking
            // user has to verify change and save it manually
            var response = await _client.GetAsync("api/pantryitem/search?query=butternut");
            response.EnsureSuccessStatusCode();
            var pantryResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(pantryResult);
            Assert.Single(pantryResult.Items);
            var quantity = pantryResult.Items.First().Quantity;

            response = await _client.GetAsync($"api/mealplan/1/cook/1");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);

            // the only seeded pantry item we can match is the butternut squash
            Assert.Equal(6, result.Items.First().FoodId);
            Assert.Equal(quantity, result.Items.First().Quantity);
        }

        [Fact]
        public async Task CookMealPlans_Returns_Unauthorized_onNoToken()
        {
            var response = await _client.GetAsync($"api/mealplan/1/cook/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CookMealPlans_Returns_NotFound_onInvalidId()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync($"api/mealplan/99/cook/1");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

            response = await _client.GetAsync($"api/mealplan/1/cook/99");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}