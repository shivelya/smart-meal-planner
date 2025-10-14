using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    [Collection("Database collection")]
    public class RecipeServiceTests
    {
        private readonly PlannerContext _context;
        private readonly RecipeService _service;
        private readonly Mock<ILogger<RecipeService>> _loggerMock;

        public RecipeServiceTests(SqliteTestFixture fixture)
        {
            _context = fixture.CreateContext();
            _loggerMock = new Mock<ILogger<RecipeService>>();
            _service = new RecipeService(_context, _loggerMock.Object);


            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();

            _context.Users.Add(new User { Id = 1, Email = "", PasswordHash = "" });
            var a = _context.SaveChanges();
        }

        [Fact]
        public async Task CreateAsync_ThrowsValidationException_WhenRequiredFieldsMissing()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Title = "", Instructions = "", Ingredients = null!, Source = null! };
            await Assert.ThrowsAsync<ValidationException>(() => _service.CreateAsync(dto, 1));
        }

        [Fact]
        public async Task CreateAsync_SuccessfullyCreatesRecipe()
        {
            var category = new Category { Id = 1, Name = "TestCat" };
            _context.Categories.Add(category);
            _context.SaveChanges();
            var dto = new CreateUpdateRecipeDtoRequest
            {
                Title = "Test",
                Source = "Source",
                Instructions = "Do stuff",
                Ingredients =
                [
                    new() { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Ing", CategoryId = 1 }, Quantity = 1, Unit = "g" }
                ]
            };

            var result = await _service.CreateAsync(dto, 1);
            Assert.NotNull(result);
            Assert.Equal("Test", result.Title);
            Assert.Equal("Source", result.Source);
            Assert.Equal("Do stuff", result.Instructions);
            Assert.Single(result.Ingredients);
            Assert.Equal("Ing", result.Ingredients[0].Food.Name);
            Assert.Equal(1, result.Ingredients[0].Quantity);
            Assert.Equal("g", result.Ingredients[0].Unit);

            var dbresult = await _context.Recipes.FindAsync(result.Id);
            Assert.NotNull(dbresult);
            Assert.Equal("Test", dbresult.Title);
            Assert.Equal("Source", dbresult.Source);
            Assert.Equal("Do stuff", dbresult.Instructions);
            Assert.Single(dbresult.Ingredients);
            Assert.Equal("Ing", dbresult.Ingredients.First().Food.Name);
            Assert.Equal(1, dbresult.Ingredients.First().Quantity);
            Assert.Equal("g", dbresult.Ingredients.First().Unit);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_WhenRecipeNotFound()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAsync(999, 1));
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_WhenUserNotOwner()
        {
            var fakeUser = new User { Id = 2, Email = "test@example.com", PasswordHash = "asdf",  };
            _context.Users.Add(fakeUser);
            _context.SaveChanges();
            var recipe = new Recipe { Id = 1, UserId = 2, Title = "T", Source = "S", Instructions = "I" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAsync(1, 1));
        }

        [Fact]
        public async Task DeleteAsync_DeletesRecipe()
        {
            var recipe = new Recipe { Id = 2, UserId = 1, Title = "T", Source = "S", Instructions = "I" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = await _service.DeleteAsync(2, 1);
            Assert.True(result);
            Assert.Empty(_context.Recipes);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
        {
            var result = await _service.GetByIdAsync(999, 1);
            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdAsync_ReturnsFullRecipe()
        {
            var recipe = new Recipe
            {
                Id = 3,
                UserId = 1,
                Title = "T",
                Source = "S",
                Instructions = "I",
                Ingredients = [new RecipeIngredient
                {
                    Food = new Food
                    {
                        Id = 1,
                        Name = "F",
                        CategoryId = 1,
                        Category = new Category { Id = 1, Name = "C" }
                    },
                    Quantity = 1,
                    Unit = "g"
                }]
            };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = await _service.GetByIdAsync(3, 1);

            Assert.NotNull(result);
            Assert.Equal("T", result.Title);
            Assert.Equal("S", result.Source);
            Assert.Equal("I", result.Instructions);
            Assert.Single(result.Ingredients);
            Assert.Equal("F", result.Ingredients[0].Food.Name);
            Assert.Equal("C", result.Ingredients[0].Food.Category.Name);
        }

        [Fact]
        public async Task GetByIdsAsync_ReturnsEmpty_WhenNoIds()
        {
            var result = await _service.GetByIdsAsync([], 1);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetByIdsAsync_ReturnsRecipes()
        {
            var recipe1 = new Recipe
            {
                Id = 4,
                UserId = 1,
                Title = "A",
                Source = "S",
                Instructions = "I",
                Ingredients = [new RecipeIngredient
                {
                    Food = new Food
                    {
                        Id = 1,
                        Name = "F",
                        CategoryId = 1,
                        Category = new Category { Id = 1, Name = "C" }
                    },
                    Quantity = 1,
                    Unit = "g"
                }]
            };
            var recipe2 = new Recipe
            {
                Id = 5,
                UserId = 1,
                Title = "B",
                Source = "S",
                Instructions = "I",
                Ingredients = [new RecipeIngredient
                {
                    Food = new Food
                    {
                        Id = 2,
                        Name = "G",
                        CategoryId = 2,
                        Category = new Category { Id = 2, Name = "D" }
                    },
                    Quantity = 1,
                    Unit = "g"
                }]
            };
            _context.Recipes.AddRange(recipe1, recipe2);
            _context.SaveChanges();

            var result = await _service.GetByIdsAsync([4, 5], 1);

            Assert.Equal(2, result.TotalCount);
            Assert.Equal("A", result.Items.First().Title);
            Assert.Equal("S", result.Items.First().Source);
            Assert.Equal("I", result.Items.First().Instructions);
            Assert.Single(result.Items.First().Ingredients);
            Assert.Equal("F", result.Items.First().Ingredients[0].Food.Name);
            Assert.Equal("C", result.Items.First().Ingredients[0].Food.Category.Name);

            Assert.Equal("B", result.Items.Last().Title);
            Assert.Equal("S", result.Items.Last().Source);
            Assert.Equal("I", result.Items.Last().Instructions);
            Assert.Single(result.Items.Last().Ingredients);
            Assert.Equal("G", result.Items.Last().Ingredients[0].Food.Name);
            Assert.Equal("D", result.Items.Last().Ingredients[0].Food.Category.Name);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByTitle()
        {
            _context.Recipes.Add(new Recipe { Id = 6, UserId = 1, Title = "Pizza", Source = "S", Instructions = "I" });
            _context.Recipes.Add(new Recipe { Id = 7, UserId = 1, Title = "Burger", Source = "S", Instructions = "I" });
            _context.SaveChanges();

            var result = await _service.SearchAsync(1, "Pizza", null, null, null);

            Assert.Single(result.Items);
            Assert.Equal("Pizza", result.Items.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByTitle_CaseInsensitive()
        {
            _context.Recipes.Add(new Recipe { Id = 6, UserId = 1, Title = "Pizza", Source = "S", Instructions = "I" });
            _context.Recipes.Add(new Recipe { Id = 7, UserId = 1, Title = "Burger", Source = "S", Instructions = "I" });
            _context.SaveChanges();

            var result = await _service.SearchAsync(1, "pizza", null, null, null);

            Assert.Single(result.Items);
            Assert.Equal("Pizza", result.Items.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByIngredient()
        {
            var food = new Food { Id = 1, Name = "Tomato", CategoryId = 1, Category = new Category { Id = 1, Name = "Veg" } };
            var recipe = new Recipe {
                Id = 10, UserId = 1, Title = "Salad", Source = "S", Instructions = "I",
                Ingredients = [new RecipeIngredient { Food = food, FoodId = 1, Quantity = 1, Unit = "g" }]
            };
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = await _service.SearchAsync(1, null, "Tomato", null, null);
            Assert.Single(result.Items);
            Assert.Equal("Salad", result.Items.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByIngredient_CaseInsensitive()
        {
            var food = new Food { Id = 1, Name = "Tomato", CategoryId = 1, Category = new Category { Id = 1, Name = "Veg" } };
            var recipe = new Recipe {
                Id = 10, UserId = 1, Title = "Salad", Source = "S", Instructions = "I",
                Ingredients = [new RecipeIngredient { Food = food, FoodId = 1, Quantity = 1, Unit = "g" }]
            };
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = await _service.SearchAsync(1, null, "tomato", null, null);
            Assert.Single(result.Items);
            Assert.Equal("Salad", result.Items.First().Title);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByTitleAndIngredient()
        {
            var food = new Food { Id = 2, Name = "Cheese", CategoryId = 1, Category = new Category { Id = 1, Name = "Veg" } };
            var recipe = new Recipe {
                Id = 11, UserId = 1, Title = "Cheese Pizza", Source = "S", Instructions = "I",
                Ingredients = [new RecipeIngredient { Food = food, FoodId = 2, Quantity = 1, Unit = "g" }]
            };
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = await _service.SearchAsync(1, "Pizza", "Cheese", null, null);
            Assert.Single(result.Items);
            Assert.Equal("Cheese Pizza", result.Items.First().Title);
        }

        [Fact]
        public async Task SearchAsync_Pagination_WorksWithSkipAndTake()
        {
            for (int i = 0; i < 5; i++)
            {
                _context.Recipes.Add(new Recipe { Id = 20 + i, UserId = 1, Title = $"Recipe{i}", Source = "S", Instructions = "I" });
            }
            _context.SaveChanges();
            var result = await _service.SearchAsync(1, null, null, 2, 2);
            Assert.Equal(5, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal("Recipe2", result.Items.First().Title);
            Assert.Equal("Recipe3", result.Items.Last().Title);
        }

        [Fact]
        public async Task SearchAsync_ThrowsArgumentException_WhenSkipIsNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SearchAsync(1, null, null, -1, null));
        }

        [Fact]
        public async Task SearchAsync_ThrowsArgumentException_WhenTakeIsNegative()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.SearchAsync(1, null, null, null, -2));
        }

        [Fact]
        public async Task SearchAsync_ReturnsEmpty_WhenNoMatches()
        {
            var result = await _service.SearchAsync(1, "Nonexistent", "Nonexistent", null, null);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchAsync_ReturnsAllRecipes_WhenParamsNullOrEmpty()
        {
            _context.Recipes.Add(new Recipe { Id = 30, UserId = 1, Title = "A", Source = "S", Instructions = "I" });
            _context.Recipes.Add(new Recipe { Id = 31, UserId = 1, Title = "B", Source = "S", Instructions = "I" });
            _context.SaveChanges();
            var result = await _service.SearchAsync(1, null, null, null, null);
            Assert.True(result.Items.Count() >= 2);
            var result2 = await _service.SearchAsync(1, "", "", null, null);
            Assert.Equal(result.Items.Count(), result2.Items.Count());
        }

        [Fact]
        public async Task SearchAsync_ReturnsIngredientFoodAndCategoryProperties()
        {
            var category = new Category { Id = 100, Name = "TestCategory" };
            var food = new Food { Id = 200, Name = "TestFood", CategoryId = 100, Category = category };
            var ingredient = new RecipeIngredient { Id = 300, FoodId = 200, Food = food, Quantity = 2.5m, Unit = "kg" };
            var recipe = new Recipe {
                Id = 400, UserId = 1, Title = "TestRecipe", Source = "S", Instructions = "I",
                Ingredients = [ingredient]
            };
            _context.Categories.Add(category);
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = await _service.SearchAsync(1, "TestRecipe", null, null, null);
            var recipeDto = result.Items.FirstOrDefault();
            Assert.NotNull(recipeDto);
            Assert.Equal("TestRecipe", recipeDto.Title);
            Assert.Single(recipeDto.Ingredients);
            var ingredientDto = recipeDto.Ingredients.First();
            Assert.Equal(2.5m, ingredientDto.Quantity);
            Assert.Equal("kg", ingredientDto.Unit);
            Assert.NotNull(ingredientDto.Food);
            Assert.Equal("TestFood", ingredientDto.Food.Name);
            Assert.NotNull(ingredientDto.Food.Category);
            Assert.Equal("TestCategory", ingredientDto.Food.Category.Name);
        }

        [Fact]
        public async Task UpdateAsync_ThrowsArgumentException_WhenRecipeNotFound()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Id = 999, Title = "T", Source = "S", Instructions = "I", Ingredients = [] };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(999, dto, 1));
        }

        [Fact]
        public async Task UpdateAsync_ThrowsValidationException_WhenUserNotOwner()
        {
            _context.Users.Add(new User { Id = 2, Email = "test@example", PasswordHash = "hash" });
            _context.SaveChanges();
            var recipe = new Recipe { Id = 8, UserId = 2, Title = "T", Source = "S", Instructions = "I", Ingredients = [] };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var dto = new CreateUpdateRecipeDtoRequest { Id = 8, Title = "T", Source = "S", Instructions = "I", Ingredients = [] };
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateAsync(8, dto, 1));
        }

        [Fact]
        public async Task UpdateAsync_UpdatesRecipe()
        {
            var recipe = new Recipe { Id = 9, UserId = 1, Title = "Old", Source = "S", Instructions = "I", Ingredients = [] };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var category = new Category { Id = 2, Name = "Cat" };
            _context.Categories.Add(category);
            _context.SaveChanges();
            var dto = new CreateUpdateRecipeDtoRequest
            {
                Id = 9,
                Title = "New",
                Source = "S2",
                Instructions = "I2",
                Ingredients = [new() { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Ing", CategoryId = 2 }, Quantity = 1, Unit = "g" }]
            };

            var result = await _service.UpdateAsync(9, dto, 1);

            Assert.NotNull(result);
            Assert.Equal(9, result.Id);
            Assert.Equal("New", result.Title);
            Assert.Equal("S2", result.Source);
            Assert.Equal("I2", result.Instructions);
            Assert.Single(result.Ingredients);
            Assert.Equal(1, result.Ingredients[0].Quantity);
            Assert.Equal("g", result.Ingredients[0].Unit);
            Assert.Equal(1, result.Ingredients[0].Food.Id); // New food should get ID 1 in the in-memory DB
            Assert.Equal("Ing", result.Ingredients[0].Food.Name);
            Assert.Equal(2, result.Ingredients[0].Food.Category.Id);
            Assert.Equal("Cat", result.Ingredients[0].Food.Category.Name);

            var dbresult = await _context.Recipes.FindAsync(9);
            Assert.NotNull(dbresult);
            Assert.Equal(9, dbresult.Id);
            Assert.Equal("New", dbresult.Title);
            Assert.Equal("S2", dbresult.Source);
            Assert.Equal("I2", dbresult.Instructions);
            Assert.Single(dbresult.Ingredients);
            Assert.Equal(1, dbresult.Ingredients.First().Quantity);
            Assert.Equal("g", dbresult.Ingredients.First().Unit);
            Assert.Equal(1, dbresult.Ingredients.First().Food.Id); // New food should get ID 1 in the in-memory DB
            Assert.Equal("Ing", dbresult.Ingredients.First().Food.Name);
            Assert.Equal(2, dbresult.Ingredients.First().Food.Category.Id);
            Assert.Equal("Cat", dbresult.Ingredients.First().Food.Category.Name);
        }

         [Fact]
        public void CookRecipe_Throws_WhenRecipeIdInvalid()
        {
            Assert.Throws<ArgumentException>(() => _service.CookRecipe(999, 1));
        }

        [Fact]
        public void CookRecipe_Throws_WhenUserIdDoesNotMatch()
        {
            _context.Users.Add(new User { Id = 2, Email = "test@example.com", PasswordHash = "hash" });
            _context.SaveChanges();
            var recipe = new Recipe { Id = 1, UserId = 2, Ingredients = [], Source = "", Title = "", Instructions = "" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            Assert.Throws<ArgumentException>(() => _service.CookRecipe(1, 1));
        }

        [Fact]
        public void CookRecipe_ReturnsUsedPantryItems()
        {
            var food = new Food { Id = 1, Name = "Egg", CategoryId = 1, Category = new Category { Id = 1, Name = "test" } };
            var pantryItem = new PantryItem { Id = 1, UserId = 1, FoodId = 1, Food = food, Quantity = 2 };
            var recipe = new Recipe
            {
                Id = 1,
                UserId = 1,
                Source = "",
                Title = "",
                Instructions = "",
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ]
            };
            _context.PantryItems.Add(pantryItem);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _service.CookRecipe(1, 1);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(pantryItem.Id, result.Items.First().Id);
            Assert.NotNull(result.Items.First().Food);
            Assert.Equal("Egg", result.Items.First().Food.Name);
            Assert.NotNull(result.Items.First().Food.Category);
            Assert.Equal("test", result.Items.First().Food.Category.Name);
        }

        [Fact]
        public void CookRecipe_ReturnsEmpty_WhenNoMatchingPantryItems()
        {
            var category = new Category { Id = 1, Name = "test" };
            _context.Categories.Add(category);
            var food = new Food { Id = 1, Name = "Egg", CategoryId = 1 };
            var pantryItem = new PantryItem { Id = 1, UserId = 1, FoodId = 1, Food = food, Quantity = 2 };
            var recipe = new Recipe {
                Id = 1,
                UserId = 1,
                Source = "",
                Title = "",
                Instructions = "",
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 2, Food = new Food { Id = 2, Name = "Milk", CategoryId = 1 }, Quantity = 1 }
                ]
            };
            _context.PantryItems.Add(pantryItem);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _service.CookRecipe(1, 1);

            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }
    }
}
