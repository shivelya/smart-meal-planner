using Backend.Model;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using Backend.DTOs;
using Backend.Services.Impl;

namespace Backend.Tests.Helpers
{
    public class ManualRecipeGeneratorTests : IDisposable
    {
        private readonly PlannerContext _context;
        private readonly RecipeGeneratorService _generator;
        private readonly ILogger<RecipeGeneratorService> _logger;
        private readonly Mock<IExternalRecipeGenerator> _externalGeneratorMock;

        public ManualRecipeGeneratorTests()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<RecipeGeneratorService>();
            _context = new PlannerContext(options, config, loggerFactory.CreateLogger<PlannerContext>());
            _externalGeneratorMock = new Mock<IExternalRecipeGenerator>();
            var generators = new List<IExternalRecipeGenerator> { _externalGeneratorMock.Object };
            _generator = new RecipeGeneratorService(_context, _logger, generators);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            _context.Dispose();
        }

        [Fact]
        public async Task GenerateMealPlan_ReturnsEmpty_WhenNoRecipes()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlanAsync(2, 1, false);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public async Task GenerateMealPlan_ReturnsEmpty_WhenNoPantryItems()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var recipe = new Recipe { Id = 1, UserId = 1, Ingredients = [], Instructions = "", Source = "", Title = "" };
            _context.Users.Add(user);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlanAsync(2, 1, false);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public async Task GenerateMealPlan_SelectsRecipe_WhenIngredientsInPantry()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var category = new Category { Id = 1, Name = "" };
            var food = new Food { Id = 1, Name = "Egg", CategoryId = 1, Category = category };
            var pantry = new PantryItem { Id = 1, UserId = 1, FoodId = 1, Food = food, Quantity = 2 };
            var ingredient = new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 };
            var recipe = new Recipe
            {
                Id = 1,
                UserId = 1,
                Ingredients = [
                    ingredient
                ],
                Instructions = "",
                Source = "",
                Title = ""
            };
            _context.Users.Add(user);
            _context.PantryItems.Add(pantry);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlanAsync(1, 1, false);
            Assert.Single(result.Meals);
            Assert.Equal(recipe.Id, result.Meals.First().RecipeId);
        }

        [Fact]
        public async Task GenerateMealPlan_SkipsRecipe_WhenIngredientNotInPantry()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var food = new Food { Id = 1, Name = "Egg" };
            var recipe = new Recipe
            {
                Id = 1,
                UserId = 1,
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ],
                Instructions = "",
                Source = "",
                Title = ""
            };
            _context.Users.Add(user);
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlanAsync(1, 1, false);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public void ScoreRecipe_ReturnsCorrectScore()
        {
            var food = new Food { Id = 1, Name = "Egg" };
            var pantry = new List<PantryItem> {
                    new() { Id = 1, FoodId = 1, Food = food, Quantity = 2 }
                };
            var recipe = new Recipe
            {
                Id = 1,
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ]
            };
            var score = typeof(RecipeGeneratorService)
                .GetMethod("ScoreRecipe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, [recipe, pantry]);
            Assert.Equal(2, score);
        }

        [Fact]
        public async Task GenerateMealPlan_ReturnsEmpty_WhenNoRecipesOrPantry()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlanAsync(2, 1, false);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public async Task GenerateMealPlan_UsesExternalGenerator_WhenManualInsufficient()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            _externalGeneratorMock.Setup(g => g.GenerateMealPlanAsync(2, It.IsAny<IQueryable<PantryItem>>()))
                .ReturnsAsync([new GeneratedMealPlanEntryDto { Title = "test", Source = "", Instructions = "" }]);

            var result = await _generator.GenerateMealPlanAsync(2, 1, false);

            Assert.Single(result.Meals);
            var meal = Assert.IsType<GeneratedMealPlanEntryDto>(result.Meals.First());
            Assert.Equal("test", meal.Title);
        }

        [Fact]
        public async Task GenerateMealPlan_SkipsManualRecipes_WhenUseExternalIsTrue()
        {
            // Arrange: Add a recipe and matching pantry item to the context
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var category = new Category { Id = 1, Name = "" };
            var food = new Food { Id = 1, Name = "Egg", CategoryId = 1, Category = category };
            var pantry = new PantryItem { Id = 1, UserId = 1, FoodId = 1, Food = food, Quantity = 2 };
            var ingredient = new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 };
            var recipe = new Recipe {
                Id = 1,
                UserId = 1,
                Ingredients = [ingredient],
                Instructions = "", Source = "", Title = "ManualRecipe"
            };
            _context.Users.Add(user);
            _context.PantryItems.Add(pantry);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();

            // Setup external generator to return a different recipe
            _externalGeneratorMock.Setup(g => g.GenerateMealPlanAsync(1, It.IsAny<IQueryable<PantryItem>>()))
                .ReturnsAsync([
                    new() {
                        Source = "Spoonacular",
                        Title = "External",
                        Instructions = ""
                    }
                ]);

            // Act: Call with useExternal = true
            var result = await _generator.GenerateMealPlanAsync(1, 1, true);

            // Assert: Only the external recipe is used, not the manual one
            Assert.Single(result.Meals);
            var gen = Assert.IsType<GeneratedMealPlanEntryDto>(result.Meals.First());
            Assert.Equal("External", gen.Title);
            Assert.Contains("Spoonacular", gen.Source);
        }
    }
}
