using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class MealPlanTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public MealPlanTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
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
            Assert.Equal(2, result.Items.Count());
            var mealPlans = result.Items.ToList();
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

            var response = await _client.GetAsync("/api/mealplan?skip=0&take=10");
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            int? firstId = null;
            int? secondId = null;
            foreach (var m in result.Items)
            {
                if (firstId == null)
                {
                    firstId = m.Id;
                    continue;
                }
                if (secondId == null)
                {
                    secondId = m.Id;
                    continue;
                }
            }

            // delete the twop plans that are seeded
            response = await _client.DeleteAsync($"/api/mealplan/{firstId}");
            response.EnsureSuccessStatusCode();
            response = await _client.DeleteAsync($"/api/mealplan/{secondId}");
            response.EnsureSuccessStatusCode();

            response = await _client.GetAsync("/api/mealplan");
            response.EnsureSuccessStatusCode();

            result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetMealPlans_Returns_pages_whenRequested()
        {
            await _factory.LoginAsync(_client);

            // delete meals so we know how many to expect once we add some
            var getResponse = await _client.GetAsync($"api/mealplan?skip=0&take=5");
            getResponse.EnsureSuccessStatusCode();
            var result = await getResponse.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);

            foreach (var m in result.Items)
            {
                var r = await _client.DeleteAsync($"api/mealplan/{m.Id}");
                r.EnsureSuccessStatusCode();
            }

            // get a meal so we know its id
            getResponse = await _client.GetAsync("api/recipe/search?title=butternut");
            getResponse.EnsureSuccessStatusCode();
            var recipeResult = await getResponse.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(recipeResult);
            var id = recipeResult.Items.First().Id;

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
                             RecipeId = id
                        }
                    ]
                });

                response.EnsureSuccessStatusCode();
            }

            //ask for 5 at a time
            for (int i = 0; i < 21; i += 5)
            {
                getResponse = await _client.GetAsync($"api/mealplan?skip={i}&take=5");

                getResponse.EnsureSuccessStatusCode();
                result = await getResponse.Content.ReadFromJsonAsync<GetMealPlansResult>();
                Assert.NotNull(result);
                Assert.Equal(17, result.TotalCount);

                //first 3 times it should return 5 items. Last time it should return zero but not throw.
                if (i <= 10)
                    Assert.Equal(5, result.Items.Count());
                else if (i < 16)
                    Assert.Equal(2, result.Items.Count());
                else
                    Assert.Empty(result.Items);
            }
        }

        [Fact]
        public async Task AddMealPlans_Returns_DTO_onSuccess()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/recipe/search?title=butternut");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);

            response = await _client.PostAsJsonAsync("api/mealplan", new CreateUpdateMealPlanRequestDto
            {
                StartDate = DateTime.UtcNow,
                Meals =
                    [
                        new CreateUpdateMealPlanEntryRequestDto
                        {
                             Notes = "new meal",
                             RecipeId = searchResult.Items.First().Id
                        }
                    ]
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<MealPlanDto>();

            Assert.NotNull(result);
            Assert.NotEqual(0, result.Id);
            Assert.Single(result.Meals);

            // ensure we don't include full object, so we have recipe id but not the recipe
            Assert.NotEqual(0, result.Meals.First().RecipeId);
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
            var mealPlanDto = result.Items.FirstOrDefault(m => m.Meals.Count() == 1);
            Assert.NotNull(mealPlanDto);

            // add a recipe to it
            var firstMeal = mealPlanDto.Meals.First();
            var mealPlanToUpdate = new CreateUpdateMealPlanRequestDto
            {
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
            var mealToCheck = result.Items.First(m => m.Id == newId);
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

        [Fact]
        public async Task DeleteMealPlans_Returns_NoContent_onSuccess()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            var id = result.Items.First().Id;

            response = await _client.DeleteAsync($"api/mealplan/{id}");
            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // verify we can no longer find that meal plan
            response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            result = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(result);
            Assert.DoesNotContain(id, result.Items.Select(m => m.Id));
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

        [Fact]
        public async Task GenerateMealPlans_FailsGracefully_WhenExternalSourceIsDown()
        {
            await _factory.LoginAsync(_client);

            using var scope = _factory.Services.CreateScope();
            var fakeGenerator = scope.ServiceProvider.GetRequiredService<FakeMealPlanGenerator>();
            fakeGenerator.ShouldThrow = true;

            var response = await _client.PostAsJsonAsync("api/mealplan/generate", new GenerateMealPlanRequestDto
            {
                UseExternal = true,
                Days = 10
            });

            fakeGenerator.ShouldThrow = false;
            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }

        [Fact]
        public async Task CookMealPlans_Returns_ListofItems_onSuccess()
        {
            await _factory.LoginAsync(_client);

            // we need to seed a mealplan to ensure there is one for our call
            var response = await _client.GetAsync("api/recipe/search?title=butternut");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);

            response = await _client.PostAsJsonAsync("api/mealplan",
                new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto { RecipeId = searchResult.Items.First().Id }] });
            response.EnsureSuccessStatusCode();
            var mealPlanResult = await response.Content.ReadFromJsonAsync<MealPlanDto>();
            Assert.NotNull(mealPlanResult);

            var id = mealPlanResult.Id;
            var entryId = mealPlanResult.Meals.First().Id;

            // quantities shouldn't change before and after cooking
            // user has to verify change and save it manually
            response = await _client.GetAsync("api/pantryitem?query=butternut");
            response.EnsureSuccessStatusCode();
            var pantryResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(pantryResult);
            Assert.Single(pantryResult.Items);
            var quantity = pantryResult.Items.First().Quantity;

            response = await _client.GetAsync($"api/mealplan/{id}/cook/{entryId}");
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(result);
            Assert.Single(result.Items);
            Assert.Equal(1, result.TotalCount);

            // the only seeded pantry item we can match is the butternut squash
            Assert.Contains("butternut", result.Items.First().Food.Name.ToLower());
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

        [Fact]
        public async Task CookMealPlans_withRecipeWithNoPantryItems_returnsEmptyArray()
        {
            await _factory.LoginAsync(_client);

            // add recipe with no current pantry items
            var response = await _client.PostAsJsonAsync("api/recipe", new CreateUpdateRecipeDtoRequest
            {
                Instructions = "i",
                Source = "s",
                Title = "t",
                Ingredients =
                [
                    new CreateUpdateRecipeIngredientDto {
                        Quantity = 1,
                        Food = new NewFoodReferenceDto {
                            CategoryId = 2,
                            Name = "not in pantry food"
                        }
                    }
                ]
            });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(result);
            var id = result.Id;

            // get meal plans
            response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            var mealPlanResult = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(mealPlanResult);
            var mealPlan = mealPlanResult.Items.First();

            // add an entry with recipe
            var meals = new List<CreateUpdateMealPlanEntryRequestDto>();
            foreach (var m in mealPlan.Meals)
            {
                meals.Add(new CreateUpdateMealPlanEntryRequestDto { Id = m.Id, Notes = m.Notes, RecipeId = m.RecipeId });
            }

            meals.Add(new CreateUpdateMealPlanEntryRequestDto { RecipeId = id });
            var request = new CreateUpdateMealPlanRequestDto
            {
                StartDate = mealPlan.StartDate,
                Meals = meals
            };

            response = await _client.PutAsJsonAsync($"api/mealplan/{mealPlan.Id}", request);
            response.EnsureSuccessStatusCode();
            var putResult = await response.Content.ReadFromJsonAsync<MealPlanDto>();
            Assert.NotNull(putResult);

            //cook said meal entry
            var entryToCook = putResult.Meals.FirstOrDefault(m => m.RecipeId == id);
            Assert.NotNull(entryToCook);

            response = await _client.GetAsync($"api/mealplan/{mealPlan.Id}/cook/{entryToCook.Id}");
            response.EnsureSuccessStatusCode();
            var itemsResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(itemsResult);
            Assert.Empty(itemsResult.Items);
            Assert.Equal(0, itemsResult.TotalCount);
        }

        [Fact]
        public async Task CookMealPlans_withEntryWithNoRecipe_returnsEmptyArray()
        {
            await _factory.LoginAsync(_client);

            // get meal plans
            var response = await _client.GetAsync("api/mealplan");
            response.EnsureSuccessStatusCode();
            var mealPlanResult = await response.Content.ReadFromJsonAsync<GetMealPlansResult>();
            Assert.NotNull(mealPlanResult);
            var mealPlan = mealPlanResult.Items.First();

            // find an entry with no recipe
            var mealPlanEntry = mealPlan.Meals.FirstOrDefault(m => m.RecipeId == null);
            Assert.NotNull(mealPlanEntry);
            var id = mealPlanEntry.Id;

            //cook said meal entry
            response = await _client.GetAsync($"api/mealplan/{mealPlan.Id}/cook/{mealPlanEntry.Id}");
            response.EnsureSuccessStatusCode();
            var itemsResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(itemsResult);
            Assert.Empty(itemsResult.Items);
            Assert.Equal(0, itemsResult.TotalCount);
        }
    }
}