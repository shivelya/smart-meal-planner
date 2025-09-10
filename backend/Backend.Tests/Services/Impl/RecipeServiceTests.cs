using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class RecipeServiceTests
    {
        private DbContextOptions<PlannerContext> _dbOptions;
        private PlannerContext _context;
        private RecipeService _service;
        private Mock<ILogger<RecipeService>> _loggerMock;
        private Mock<ILogger<PlannerContext>> _contextLoggerMock;

        public RecipeServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            _contextLoggerMock = new Mock<ILogger<PlannerContext>>();
            _context = new PlannerContext(_dbOptions, config, _contextLoggerMock.Object);
            _loggerMock = new Mock<ILogger<RecipeService>>();
            _service = new RecipeService(_context, _loggerMock.Object);
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
            Assert.Single(result.Ingredients);
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_WhenRecipeNotFound()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.DeleteAsync(999, 1));
        }

        [Fact]
        public async Task DeleteAsync_ThrowsArgumentException_WhenUserNotOwner()
        {
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
        public async Task GetByIdAsync_ReturnsRecipe()
        {
            var recipe = new Recipe { Id = 3, UserId = 1, Title = "T", Source = "S", Instructions = "I" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = await _service.GetByIdAsync(3, 1);
            Assert.NotNull(result);
            Assert.Equal("T", result.Title);
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
            var recipe1 = new Recipe { Id = 4, UserId = 1, Title = "A", Source = "S", Instructions = "I" };
            var recipe2 = new Recipe { Id = 5, UserId = 1, Title = "B", Source = "S", Instructions = "I" };
            _context.Recipes.AddRange(recipe1, recipe2);
            _context.SaveChanges();
            var result = await _service.GetByIdsAsync(new List<int> { 4, 5 }, 1);
            Assert.Equal(2, result.TotalCount);
        }

        [Fact]
        public async Task SearchAsync_ReturnsRecipes_ByTitle()
        {
            _context.Recipes.Add(new Recipe { Id = 6, UserId = 1, Title = "Pizza", Source = "S", Instructions = "I" });
            _context.Recipes.Add(new Recipe { Id = 7, UserId = 1, Title = "Burger", Source = "S", Instructions = "I" });
            _context.SaveChanges();
            var options = new RecipeSearchOptions { TitleContains = "Pizza" };
            var result = await _service.SearchAsync(options, 1);
            Assert.Single(result.Items);
            Assert.Equal("Pizza", result.Items.First().Title);
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
            var recipe = new Recipe { Id = 8, UserId = 2, Title = "T", Source = "S", Instructions = "I", Ingredients = [] };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var dto = new CreateUpdateRecipeDtoRequest { Id = 8, Title = "T", Source = "S", Instructions = "I", Ingredients = [] };
            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateAsync(8, dto, 1));
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
            var dto = new CreateUpdateRecipeDtoRequest { Id = 9, Title = "New", Source = "S", Instructions = "I",
                Ingredients = [new() { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Ing", CategoryId = 2 }, Quantity = 1, Unit = "g" }] };
            var result = await _service.UpdateAsync(9, dto, 1);
            Assert.Equal("New", result.Title);
            Assert.Single(result.Ingredients);
        }

         [Fact]
        public void CookRecipe_Throws_WhenRecipeIdInvalid()
        {
            Assert.Throws<ArgumentException>(() => _service.CookRecipe(999, 1));
        }

        [Fact]
        public void CookRecipe_Throws_WhenUserIdDoesNotMatch()
        {
            var recipe = new Recipe { Id = 1, UserId = 2, Ingredients = [], Source = "", Title = "", Instructions = "" };
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            Assert.Throws<ValidationException>(() => _service.CookRecipe(1, 1));
        }

        [Fact]
        public void CookRecipe_ReturnsUsedPantryItems()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var food = new Food { Id = 1, Name = "Egg", CategoryId = 1, Category = new Category { Id = 1, Name = "test"} };
            var pantryItem = new PantryItem { Id = 1, UserId = 1, FoodId = 1, Food = food, Quantity = 2 };
            var recipe = new Recipe {
                Id = 1,
                UserId = 1,
                Source = "",
                Title = "",
                Instructions = "",
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ]
            };
            _context.Users.Add(user);
            _context.Foods.Add(food);
            _context.PantryItems.Add(pantryItem);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();

            var result = _service.CookRecipe(1, 1);

            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(pantryItem.Id, result.Items.First().Id);
        }

        [Fact]
        public void CookRecipe_ReturnsEmpty_WhenNoMatchingPantryItems()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
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
            _context.Users.Add(user);
            _context.Foods.Add(food);
            _context.PantryItems.Add(pantryItem);
            _context.Recipes.Add(recipe);
            _context.SaveChanges();
            var result = _service.CookRecipe(1, 1);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }
    }
}
