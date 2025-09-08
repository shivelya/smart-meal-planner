using Backend.Model;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Helpers
{
    public class ManualRecipeGeneratorTests : IDisposable
    {
        private readonly PlannerContext _context;
        private readonly ManualRecipeGenerator _generator;
        private readonly ILogger<ManualRecipeGenerator> _logger;

        public ManualRecipeGeneratorTests()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            _logger = loggerFactory.CreateLogger<ManualRecipeGenerator>();
            _context = new PlannerContext(options, config, loggerFactory.CreateLogger<PlannerContext>());
            _generator = new ManualRecipeGenerator(_context, _logger);
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
            var result = await _generator.GenerateMealPlan(2, 1);
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
            var result = await _generator.GenerateMealPlan(2, 1);
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
            var recipe = new Recipe {
                Id = 1,
                UserId = 1,
                Ingredients = [
                    ingredient
                ], Instructions = "", Source = "", Title = ""
            };
            _context.Users.Add(user);
            _context.PantryItems.Add(pantry);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlan(1, 1);
            Assert.Single(result.Meals);
            Assert.Equal(recipe.Id, result.Meals.First().RecipeId);
        }

        [Fact]
        public async Task GenerateMealPlan_SkipsRecipe_WhenIngredientNotInPantry()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var food = new Food { Id = 1, Name = "Egg" };
            var recipe = new Recipe {
                Id = 1,
                UserId = 1,
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ], Instructions = "", Source = "", Title = ""
            };
            _context.Users.Add(user);
            _context.Foods.Add(food);
            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            var result = await _generator.GenerateMealPlan(1, 1);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public void ScoreRecipe_ReturnsCorrectScore()
        {
            var food = new Food { Id = 1, Name = "Egg" };
            var pantry = new List<PantryItem> {
                new() { Id = 1, FoodId = 1, Food = food, Quantity = 2 }
            };
            var recipe = new Recipe {
                Id = 1,
                Ingredients = [
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ]
            };
            var score = typeof(ManualRecipeGenerator)
                .GetMethod("ScoreRecipe", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                .Invoke(null, [recipe, pantry]);
            Assert.Equal(2, score);
        }
    }
}
