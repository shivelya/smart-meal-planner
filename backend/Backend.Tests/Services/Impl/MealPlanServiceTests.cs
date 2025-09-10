using System.ComponentModel.DataAnnotations;
using System.Security;
using Backend.DTOs;
using Backend.Model;
using Backend.Services.Impl;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Services.Impl
{
    public class MealPlanServiceTests
    {
        private readonly PlannerContext plannerContext;
        private readonly Mock<ILogger<MealPlanService>> _mockLogger;
        private readonly Mock<IRecipeGenerator> _mockRecipeGenerator;
        private readonly MealPlanService _service;

        public MealPlanServiceTests()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            var logger = new Mock<ILogger<PlannerContext>>();
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            plannerContext = new PlannerContext(options, config, logger.Object);
            _mockLogger = new Mock<ILogger<MealPlanService>>();
            _mockRecipeGenerator = new Mock<IRecipeGenerator>();
            _service = new MealPlanService(plannerContext, _mockLogger.Object, _mockRecipeGenerator.Object);
        }

        [Fact]
        public async Task GetMealPlansAsync_ThrowsOnNegativeSkip()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMealPlansAsync(-1, 10));
        }

        [Fact]
        public async Task GetMealPlansAsync_ThrowsOnNonPositiveTake()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMealPlansAsync(0, 0));
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GetMealPlansAsync(0, -1));
        }

        [Fact]
        public async Task AddMealPlanAsync_ThrowsIfUserNotFound()
        {
            var req = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto()] };
            await Assert.ThrowsAsync<SecurityException>(() => _service.AddMealPlanAsync(1, req));
        }

        [Fact]
        public async Task AddMealPlanAsync_ThrowsIfMealsEmpty()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            plannerContext.Users.Add(user);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto { Meals = [] };

            await Assert.ThrowsAsync<ValidationException>(() => _service.AddMealPlanAsync(1, req));
        }

        [Fact]
        public async Task AddMealPlanAsync_AddsMealPlan()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            plannerContext.Users.Add(user);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto { Notes = "n", RecipeId = 2 }] };

            var result = await _service.AddMealPlanAsync(1, req);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
        }

        [Fact]
        public async Task UpdateMealPlanAsync_ThrowsIfUserNotFound()
        {
            var req = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto()] };
            await Assert.ThrowsAsync<SecurityException>(() => _service.UpdateMealPlanAsync(1, 1, req));
        }

        [Fact]
        public async Task UpdateMealPlanAsync_ThrowsIfMealPlanNotFound()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            plannerContext.Users.Add(user);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto()] };
            await Assert.ThrowsAsync<SecurityException>(() => _service.UpdateMealPlanAsync(1, 1, req));
        }

        [Fact]
        public async Task UpdateMealPlanAsync_ThrowsIfRequestIdMismatch()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1, Meals = new List<MealPlanEntry>() };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto { Id = 2, Meals = [new CreateUpdateMealPlanEntryRequestDto()] };

            await Assert.ThrowsAsync<SecurityException>(() => _service.UpdateMealPlanAsync(1, 1, req));
        }

        [Fact]
        public async Task UpdateMealPlanAsync_ThrowsIfUpdatesToNonExistentMeals()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1, Meals = [new MealPlanEntry { Id = 1, Notes = "spaghetti night" }] };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto
            {
                Id = 1,
                Meals = [
                new CreateUpdateMealPlanEntryRequestDto { Id = 1, RecipeId = 42 }]
            };

            await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateMealPlanAsync(1, 1, req));
        }

        [Fact]
        public async Task UpdateMealPlanAsync_UpdatesAddsAndDeletedAtOnce()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var recipe = new Recipe { Id = 1, Source = "test", Title = "title", Instructions = "do this", UserId = 1 };
            var recipe2 = new Recipe { Id = 2, Source = "test2", Title = "title2", Instructions = "do this2", UserId = 1 };
            plannerContext.Recipes.Add(recipe);
            plannerContext.Recipes.Add(recipe2);
            var mealPlan = new MealPlan
            {
                Id = 1,
                UserId = 1,
                Meals = [
                new MealPlanEntry { Id = 1, Notes = "spaghetti night", RecipeId = 1, Recipe = recipe },
                new MealPlanEntry { Id = 22, Notes = "to be deleted" }]
            };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();

            var req = new CreateUpdateMealPlanRequestDto
            {
                Id = 1,
                Meals = [
                new CreateUpdateMealPlanEntryRequestDto { Id = 1, Notes = "n" },
                new CreateUpdateMealPlanEntryRequestDto { Notes = "to be added", RecipeId = 2 }]
            };

            var result = await _service.UpdateMealPlanAsync(1, 1, req);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(2, result.Meals.Count());
            Assert.Equal("n", result.Meals.Where(m => m.Id == 1).First().Notes);
            Assert.Null(result.Meals.Where(m => m.Id == 1).First().RecipeId);
            Assert.DoesNotContain(22, result.Meals.Select(m => m.Id));
            Assert.Equal("to be added", result.Meals.Where(m => m.Id != 1).First().Notes);
            Assert.Equal(2, result.Meals.Where(m => m.Id != 1).First().RecipeId);
        }

        [Fact]
        public async Task UpdateMealPlanAsync_UpdatesExistingMeals()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1, Meals = [new MealPlanEntry { Id = 1, Notes = "spaghetti night" }] };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto
            {
                Id = 1,
                Meals = [
                new CreateUpdateMealPlanEntryRequestDto { Id = 1, Notes = "n" }]
            };

            var result = await _service.UpdateMealPlanAsync(1, 1, req);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            var entry = Assert.Single(result.Meals);
            Assert.Equal("n", entry.Notes);
            Assert.Null(entry.RecipeId);
        }

        [Fact]
        public async Task UpdateMealPlanAsync_AddsNewMeals()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1, Meals = [] };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            var recipe = new Recipe { Id = 2, Source = "test", Title = "title", Instructions = "do this", UserId = 1 };
            plannerContext.Recipes.Add(recipe);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto
            {
                Id = 1,
                Meals = [
                new CreateUpdateMealPlanEntryRequestDto { Id = null, Notes = "n", RecipeId = 2 }]
            };

            var result = await _service.UpdateMealPlanAsync(1, 1, req);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            var entry = Assert.Single(result.Meals);
            Assert.Equal(2, entry.RecipeId);
            Assert.Equal("n", entry.Notes);
        }

        [Fact]
        public async Task UpdateMealPlanAsync_DeletesOldMeals()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1, Meals = [new MealPlanEntry { Id = 1, Notes = "note" }] };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            var req = new CreateUpdateMealPlanRequestDto { Id = 1, Meals = [] };

            var result = await _service.UpdateMealPlanAsync(1, 1, req);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Empty(result.Meals);
        }

        [Fact]
        public async Task DeleteMealPlanAsync_ThrowsIfUserNotFound()
        {
            await Assert.ThrowsAsync<SecurityException>(() => _service.DeleteMealPlanAsync(1, 1));
        }

        [Fact]
        public async Task DeleteMealPlanAsync_ThrowsIfMealPlanNotFound()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            plannerContext.Users.Add(user);
            plannerContext.SaveChanges();

            await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteMealPlanAsync(1, 1));
        }

        [Fact]
        public async Task DeleteMealPlanAsync_ThrowsIfMealPlanNotOwned()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 2 };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();

            await Assert.ThrowsAsync<ValidationException>(() => _service.DeleteMealPlanAsync(1, 1));
        }

        [Fact]
        public async Task DeleteMealPlanAsync_DeletesMealPlan()
        {
            var user = new User { Id = 1, Email = "", PasswordHash = "" };
            var mealPlan = new MealPlan { Id = 1, UserId = 1 };
            plannerContext.Users.Add(user);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            var result = await _service.DeleteMealPlanAsync(1, 1);
            Assert.True(result);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ThrowsIfDaysLessThanOne()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateMealPlanAsync(0, 1, DateTime.Today, false));
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ReturnsGeneratedMealPlan()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [] };
            _mockRecipeGenerator.Setup(r => r.GenerateMealPlan(2, It.IsAny<int>(), false)).ReturnsAsync(mealPlan);
            var result = await _service.GenerateMealPlanAsync(2, 1, DateTime.Today, false);
            Assert.NotNull(result);
            Assert.Equal(mealPlan, result);
        }

         [Fact]
        public async Task CookMeal_Throws_WhenMealPlanIdInvalid()
        {
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CookMeal(999, 1, 1));
        }

        [Fact]
        public async Task CookMeal_Throws_WhenUserIdDoesNotMatch()
        {
            var mealPlan = new MealPlan { Id = 1, UserId = 2 };
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            await Assert.ThrowsAsync<ValidationException>(() => _service.CookMeal(1, 1, 1));
        }

        [Fact]
        public async Task CookMeal_Throws_WhenMealPlanEntryIdInvalid()
        {
            var mealPlan = new MealPlan { Id = 1, UserId = 1 };
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.SaveChanges();
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CookMeal(1, 999, 1));
        }

        [Fact]
        public async Task CookMeal_Throws_WhenMealPlanEntryNotInMealPlan()
        {
            var mealPlan = new MealPlan { Id = 1, UserId = 1 };
            var mealPlanEntry = new MealPlanEntry { Id = 2, MealPlanId = 2 };
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.MealPlanEntries.Add(mealPlanEntry);
            plannerContext.SaveChanges();
            await Assert.ThrowsAsync<ValidationException>(() => _service.CookMeal(1, 2, 1));
        }

        [Fact]
        public async Task CookMeal_ReturnsUsedPantryItems()
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
                    new RecipeIngredient { RecipeId = 1, FoodId = 1, Food = food, Quantity = 1 }
                ]
            };
            var mealPlan = new MealPlan { Id = 1, UserId = 1 };
            var mealPlanEntry = new MealPlanEntry { Id = 2, MealPlanId = 1, RecipeId = 1, Recipe = recipe };
            plannerContext.Users.Add(user);
            plannerContext.Foods.Add(food);
            plannerContext.PantryItems.Add(pantryItem);
            plannerContext.Recipes.Add(recipe);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.MealPlanEntries.Add(mealPlanEntry);
            plannerContext.SaveChanges();
            var result = await _service.CookMeal(1, 2, 1);
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal(pantryItem.Id, result.Items.First().Id);
        }

        [Fact]
        public async Task CookMeal_ReturnsEmpty_WhenNoMatchingPantryItems()
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
            var mealPlan = new MealPlan { Id = 1, UserId = 1 };
            var mealPlanEntry = new MealPlanEntry { Id = 2, MealPlanId = 1, RecipeId = 1, Recipe = recipe };
            plannerContext.Users.Add(user);
            plannerContext.Foods.Add(food);
            plannerContext.PantryItems.Add(pantryItem);
            plannerContext.Recipes.Add(recipe);
            plannerContext.MealPlans.Add(mealPlan);
            plannerContext.MealPlanEntries.Add(mealPlanEntry);
            plannerContext.SaveChanges();
            var result = await _service.CookMeal(1, 2, 1);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }
    }
}
