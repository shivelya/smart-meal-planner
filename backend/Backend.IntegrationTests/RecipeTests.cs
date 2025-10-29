using System.Net;
using System.Net.Http.Json;
using Backend.DTOs;
using Backend.Helpers;

namespace Backend.IntegrationTests
{
    [Collection("Database collection")]
    public class RecipeTests
    {
        private readonly CustomWebApplicationFactory _factory;
        private readonly HttpClient _client;

        public RecipeTests(CustomWebApplicationFactory factory)
        {
            _factory = factory;
            _factory.ResetDatabase();
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task GetRecipe_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            // search for a recipe out of seeded data to get an id to use for detail fetch
            var response = await _client.GetAsync("/api/recipe/search?title=soup");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            var recipeId = searchResult.Items.First().Id;

            response = await _client.GetAsync($"/api/recipe/{recipeId}");
            response.EnsureSuccessStatusCode();
            var recipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(recipe);
            Assert.Equal(recipeId, recipe.Id);
            Assert.NotEmpty(recipe.Ingredients);
            Assert.Equal(4, recipe.Ingredients.Count);
            Assert.NotEmpty(recipe.Instructions);
            Assert.Equal("Butternut Squash Soup with Fresh Goat Cheese", recipe.Title);
            Assert.Equal("Spoonacular - 636603", recipe.Source);
        }

        [Fact]
        public async Task GetRecipe_InvalidId_ReturnsNotFound()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.GetAsync($"/api/recipe/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetRecipe_ReturnUnauthorized_IfNotAuthenticated()
        {
            var unauthClient = _factory.CreateClient(new() { AllowAutoRedirect = false });
            var response = await unauthClient.GetAsync($"/api/recipe/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UpdateRecipe_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            // search for a recipe out of seeded data to get a recipe to update
            var response = await _client.GetAsync("/api/recipe/search?title=soup");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            var recipe = searchResult.Items.First();

            // port to an update dto
            var ingredients = recipe.Ingredients.Select(i => new CreateUpdateRecipeIngredientDto
            {
                Quantity = i.Quantity,
                Unit = i.Unit,
                Id = i.Id,
                Food = new ExistingFoodReferenceDto { Id = i.Food.Id }
            }).ToList();

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = recipe.Title,
                Source = recipe.Source!,
                Ingredients = ingredients,
                Instructions = recipe.Instructions
            };

            // make changes and send update
            recipeDto.Title = "Updated Title";
            recipeDto.Instructions += "\nEnjoy your meal!";

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            response.EnsureSuccessStatusCode();
            var updatedRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(updatedRecipe);
            Assert.Equal(recipeDto.Title, updatedRecipe.Title);
            Assert.EndsWith("Enjoy your meal!", updatedRecipe.Instructions);
        }

        [Fact]
        public async Task UpdateRecipe_InvalidId_ReturnsNotFound()
        {
            await _factory.LoginAsync(_client);

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = "Nonexistent Recipe",
                Source = "Unit Test",
                Ingredients = new List<CreateUpdateRecipeIngredientDto>(),
                Instructions = "N/A"
            };

            var response = await _client.PutAsJsonAsync($"/api/recipe/9999", recipeDto);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateRecipe_ReturnsBadRequest_WithBadData()
        {
            await _factory.LoginAsync(_client);

            // search for a recipe out of seeded data to get a recipe to update
            var response = await _client.GetAsync("/api/recipe/search?title=soup");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            var recipe = searchResult.Items.First();

            // port to an update dto
            var ingredients = recipe.Ingredients.Select(i => new CreateUpdateRecipeIngredientDto
            {
                Quantity = i.Quantity,
                Unit = i.Unit,
                Id = i.Id,
                Food = new ExistingFoodReferenceDto { Id = i.Food.Id }
            }).ToList();

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = recipe.Title,
                Source = recipe.Source!,
                Ingredients = ingredients,
                Instructions = recipe.Instructions
            };

            // make changes and send update
            recipeDto.Title = null!;

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Title = "Valid Title";
            recipeDto.Instructions = null!;

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Instructions = "Valid Instructions";
            recipeDto.Ingredients[0].Quantity = -5;

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Ingredients[0].Quantity = 1;
            recipeDto.Ingredients[0].Food = null!;

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Ingredients[0].Food = new ExistingFoodReferenceDto { Id = 9999 };

            response = await _client.PutAsJsonAsync($"/api/recipe/{recipe.Id}", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateRecipe_ReturnUnauthorized_IfNotAuthenticated()
        {
            var unauthClient = _factory.CreateClient(new() { AllowAutoRedirect = false });

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = "Some Recipe",
                Source = "Unit Test",
                Ingredients = new List<CreateUpdateRecipeIngredientDto>(),
                Instructions = "N/A"
            };

            var response = await unauthClient.PutAsJsonAsync($"/api/recipe/1", recipeDto);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Search_ByTitle_ReturnsResults()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/recipe/search?title=goat");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            Assert.All(searchResult.Items, r => Assert.Contains("goat", r.Title.ToLower()));
        }

        [Fact]
        public async Task Search_ByIngredient_ReturnsResults()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/recipe/search?ingredient=milk");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            foreach (var recipe in searchResult.Items)
                Assert.Contains(recipe.Ingredients, i => i.Food.Name.ToLower().Contains("milk"));
        }

        [Fact]
        public async Task Search_NoMatches_ReturnsEmpty()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/recipe/search?title=nonexistentrecipe");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.Empty(searchResult.Items);
            Assert.Equal(0, searchResult.TotalCount);
        }

        [Fact]
        public async Task Search_ReturnUnauthorized_IfNotAuthenticated()
        {
            var unauthClient = _factory.CreateClient(new() { AllowAutoRedirect = false });
            var response = await unauthClient.GetAsync("/api/recipe/search?title=soup");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task Search_Pagination_WorksCorrectly()
        {
            await _factory.LoginAsync(_client);

            // get a food to use in the seeded recipes
            var foodResponse = await _client.GetAsync("/api/food?query=water");
            foodResponse.EnsureSuccessStatusCode();
            var foodSearchResult = await foodResponse.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(foodSearchResult);
            Assert.NotEmpty(foodSearchResult.Items);
            var foodId = foodSearchResult.Items.First().Id;

            // seed data that has multiple recipes with 'a' in the title
            for (int i = 0; i < 5; i++)
            {
                var insertResponse = await _client.PostAsJsonAsync("/api/recipe", new CreateUpdateRecipeDtoRequest
                {
                    Title = "Apple Pie",
                    Source = "Unit Test",
                    Ingredients = new List<CreateUpdateRecipeIngredientDto> { new() {
                        Quantity = 2,
                        Unit = "pieces",
                        Food = new ExistingFoodReferenceDto { Id = foodId }
                    }},
                    Instructions = "N/A"
                });
                insertResponse.EnsureSuccessStatusCode();
            }

            var response = await _client.GetAsync("/api/recipe/search?title=a&skip=0&take=2");
            response.EnsureSuccessStatusCode();
            var searchResultPage1 = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResultPage1);
            Assert.Equal(2, searchResultPage1.Items.Count());

            response = await _client.GetAsync("/api/recipe/search?title=a&skip=2&take=2");
            response.EnsureSuccessStatusCode();
            var searchResultPage2 = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResultPage2);
            Assert.Equal(2, searchResultPage2.Items.Count());

            // Ensure no overlap between pages
            var idsPage1 = searchResultPage1.Items.Select(r => r.Id).ToHashSet();
            var idsPage2 = searchResultPage2.Items.Select(r => r.Id).ToHashSet();
            Assert.Empty(idsPage1.Intersect(idsPage2));
        }

        [Fact]
        public async Task Search_InvalidParameters_ReturnBadRequest()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/recipe/search?take=0");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.GetAsync("/api/recipe/search?skip=-1");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.GetAsync("/api/recipe/search?take=-5");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            response = await _client.GetAsync("/api/recipe/search");
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            // get a food to use in the recipe
            var foodResponse = await _client.GetAsync("/api/food?query=water");
            foodResponse.EnsureSuccessStatusCode();
            var foodSearchResult = await foodResponse.Content.ReadFromJsonAsync<GetFoodsResult>();
            Assert.NotNull(foodSearchResult);
            Assert.NotEmpty(foodSearchResult.Items);
            var foodId = foodSearchResult.Items.First().Id;

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = "New Recipe",
                Source = "Unit Test",
                Ingredients =
                [
                    new()
                    {
                        Quantity = 3,
                        Unit = "cups",
                        Food = new ExistingFoodReferenceDto { Id = foodId }
                    }
                ],
                Instructions = "Mix ingredients and cook."
            };

            var response = await _client.PostAsJsonAsync("/api/recipe", recipeDto);
            var content = response.Content;
            response.EnsureSuccessStatusCode();
            var createdRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(createdRecipe);
            Assert.Equal(recipeDto.Title, createdRecipe.Title);
            Assert.Equal(recipeDto.Instructions, createdRecipe.Instructions);
            Assert.Single(createdRecipe.Ingredients);
            Assert.Equal(3, createdRecipe.Ingredients.First().Quantity);

            // can get the recipe back via get
            response = await _client.GetAsync($"/api/recipe/{createdRecipe.Id}");
            response.EnsureSuccessStatusCode();
            var fetchedRecipe = await response.Content.ReadFromJsonAsync<RecipeDto>();
            Assert.NotNull(fetchedRecipe);
            Assert.Equal(createdRecipe.Id, fetchedRecipe.Id);
            Assert.Equal(createdRecipe.Title, fetchedRecipe.Title);
            Assert.Equal(createdRecipe.Instructions, fetchedRecipe.Instructions);
            Assert.Single(fetchedRecipe.Ingredients);
            Assert.Equal(3, fetchedRecipe.Ingredients.First().Quantity);
        }

        [Fact]
        public async Task CreateRecipe_ReturnsBadRequest_WithBadData()
        {
            await _factory.LoginAsync(_client);

            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = null!,
                Source = "Unit Test",
                Ingredients = [],
                Instructions = "N/A"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Title = "Valid Title";
            recipeDto.Instructions = null!;

            response = await _client.PostAsJsonAsync("/api/recipe", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            recipeDto.Instructions = "Valid Instructions";
            recipeDto.Ingredients = [
                new()
                {
                    Quantity = -2,
                    Unit = "cups",
                    Food = new ExistingFoodReferenceDto { Id = 1 }
                }
            ];

            response = await _client.PostAsJsonAsync("/api/recipe", recipeDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task CreateRecipe_ReturnUnauthorized_IfNotAuthenticated()
        {
            var recipeDto = new CreateUpdateRecipeDtoRequest
            {
                Title = "Some Recipe",
                Source = "Unit Test",
                Ingredients = [],
                Instructions = "N/A"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe", recipeDto);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact(Skip = "Delete check functionality not implemented yet")]
        public async Task DeleteRecipe_ReturnsNoContent()
        {
            await _factory.LoginAsync(_client);

            // search for a recipe out of seeded data to get an id to delete
            var response = await _client.GetAsync("/api/recipe/search?title=soup");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            var recipeId = searchResult.Items.First().Id;

            response = await _client.DeleteAsync($"/api/recipe/{recipeId}");
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            // ensure it's really deleted
            response = await _client.GetAsync($"/api/recipe/{recipeId}");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteRecipe_InvalidId_ReturnsNotFound()
        {
            await _factory.LoginAsync(_client);
            var response = await _client.DeleteAsync($"/api/recipe/9999");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteRecipe_ReturnUnauthorized_IfNotAuthenticated()
        {
            var unauthClient = _factory.CreateClient(new() { AllowAutoRedirect = false });
            var response = await unauthClient.DeleteAsync($"/api/recipe/1");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_FromUrl_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "https://www.allrecipes.com/recipe/273024/goat-cheese-and-sun-dried-tomato-stuffed-chicken-thighs-with-sage-brown-butter-sauce/"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            response.EnsureSuccessStatusCode();
            var extractedRecipe = await response.Content.ReadFromJsonAsync<ExtractedRecipe>();
            Assert.NotNull(extractedRecipe);
            Assert.Equal("Goat Cheese and Sun-Dried Tomato Stuffed Chicken Thighs with Sage Brown Butter Sauce", extractedRecipe.Title);
            Assert.NotEmpty(extractedRecipe.Ingredients);
            Assert.NotNull(extractedRecipe.Instructions);
            Assert.NotEmpty(extractedRecipe.Instructions);
            Assert.NotEmpty(extractedRecipe.Title);
        }

        [Fact]
        public async Task ExtractRecipe_NullUrl_ReturnsBadRequest()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = null!
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_FromMalformedUri_ReturnsBadRequest()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "https://asd lkja l/ lkja slkd"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_WithNoSchema_ReturnsOk()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "www.allrecipes.com/recipe/273024/goat-cheese-and-sun-dried-tomato-stuffed-chicken-thighs-with-sage-brown-butter-sauce/"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_WithInvalidSchema_ReturnsBadGateway()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "htps://invalid-url"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_WithInvalidUrl_ReturnsBadGateway()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "https://www.allrecipes.com/recipe/273024/goat-cheese-and-"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_WithNoPermission_ReturnsBadGateway()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "https://www.vrbo.com/" // returns a 403 when hit with a simple GET
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.BadGateway, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_FromUrlWithNoRecipe_ReturnsUnprocessableEntity()
        {
            await _factory.LoginAsync(_client);

            var requestDto = new ExtractRequest
            {
                Source = "https://ground.news/local"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnUnauthorized_IfNotAuthentnicated()
        {
            var requestDto = new ExtractRequest
            {
                Source = "https://www.allrecipes.com/recipe/273024/goat-cheese-and-sun-dried-tomato-stuffed-chicken-thighs-with-sage-brown-butter-sauce/"
            };

            var response = await _client.PostAsJsonAsync("/api/recipe/extract", requestDto);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CookRecipe_Returns_PotentialPantryItemsUsed()
        {
            await _factory.LoginAsync(_client);

            // search for a recipe out of seeded data to get an id to use for cooking
            var response = await _client.GetAsync("/api/recipe/search?title=soup");
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<GetRecipesResult>();
            Assert.NotNull(searchResult);
            Assert.NotEmpty(searchResult.Items);
            var id = searchResult.Items.First().Id;

            // get pantry item info now to verify cooking doesn't change it
            response = await _client.GetAsync("/api/pantryitem?query=butternut");
            response.EnsureSuccessStatusCode();
            var pantryResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(pantryResult);
            Assert.NotEmpty(pantryResult.Items);
            var pantryItem = pantryResult.Items.First();
            Assert.NotNull(pantryItem.Food);

            response = await _client.GetAsync($"/api/recipe/{id}/cook");
            response.EnsureSuccessStatusCode();
            var cookResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(cookResult);
            Assert.NotEmpty(cookResult.Items);
            var item = cookResult.Items.First();
            Assert.NotNull(item.Food);
            Assert.Equal(pantryItem.Quantity, item.Quantity);

            // verify pantry item is unchanged
            response = await _client.GetAsync("/api/pantryitem?query=butternut");
            response.EnsureSuccessStatusCode();
            pantryResult = await response.Content.ReadFromJsonAsync<GetPantryItemsResult>();
            Assert.NotNull(pantryResult);
            Assert.NotEmpty(pantryResult.Items);
            var newPantryItem = pantryResult.Items.First();
            Assert.NotNull(pantryItem.Food);

            Assert.Equal(pantryItem.Id, newPantryItem.Id);
            Assert.Equal(pantryItem.Quantity, newPantryItem.Quantity);
        }

        [Fact]
        public async Task CookRecipe_ReturnUnauthorized_IfNotAuthenticated()
        {
            var response = await _client.GetAsync("/api/recipe/999/cook");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task CookRecipe_InvalidId_ReturnsNotFound()
        {
            await _factory.LoginAsync(_client);

            var response = await _client.GetAsync("/api/recipe/999/cook");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}