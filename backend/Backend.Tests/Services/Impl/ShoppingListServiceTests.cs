using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Backend.Tests.Services.Impl
{
    public class ShoppingListServiceTests
    {
        private static PlannerContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var config = new ConfigurationBuilder().Build();
            var logger = new LoggerFactory().CreateLogger<PlannerContext>();
            return new PlannerContext(options, config, logger);
        }

        private static ShoppingListService CreateService(PlannerContext context)
        {
            var logger = new LoggerFactory().CreateLogger<ShoppingListService>();
            return new ShoppingListService(context, logger);
        }

        [Fact]
        public async Task GetShoppingList_ReturnsEmpty_WhenNoItemsExist()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.SaveChanges();
            var service = CreateService(context);

            var result = await service.GetShoppingListAsync(42);

            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task GetShoppingList_ReturnsItems_ForUser()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 10, Name = "Apple" });
            context.Foods.Add(new Food { Id = 20, Name = "Banana" });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, UserId = 42, FoodId = 10, Purchased = false });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 2, UserId = 42, FoodId = 20, Purchased = true });
            context.SaveChanges();
            var service = CreateService(context);

            var result = await service.GetShoppingListAsync(42);

            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Collection(result.Items,
                item => Assert.Equal(1, item.Id),
                item => Assert.Equal(2, item.Id));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_ThrowsArgumentException_WhenRequestIsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateShoppingListItemAsync(null!, 42));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_ThrowsArgumentException_WhenIdIsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = null, FoodId = 10, Purchased = true, Notes = "note" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateShoppingListItemAsync(request, 42));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_ThrowsValidationException_WhenItemNotFound()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 99, FoodId = 10, Purchased = true, Notes = "note" };
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateShoppingListItemAsync(request, 42));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_ThrowsValidationException_WhenUserNotValid()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 99, FoodId = 10, Purchased = true, Notes = "note" };
            await Assert.ThrowsAsync<ValidationException>(() => service.UpdateShoppingListItemAsync(request, 99));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_ThrowsArgumentException_WhenFoodIdIsInvalid()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, UserId = 42, FoodId = 10, Purchased = false });
            context.SaveChanges();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 1, FoodId = 999, Purchased = true, Notes = "note" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateShoppingListItemAsync(request, 42));
        }

        [Fact]
        public async Task UpdateShoppingListItemAsync_UpdatesItemSuccessfully()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 10, Name = "Apple" });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, UserId = 42, FoodId = 10, Purchased = false, Notes = "old" });
            context.SaveChanges();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 1, FoodId = 10, Purchased = true, Notes = "updated" };

            var result = await service.UpdateShoppingListItemAsync(request, 42);

            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal(10, result.FoodId);
            Assert.True(result.Purchased);
            Assert.Equal("updated", result.Notes);
            Assert.NotNull(result.Food);
            Assert.Equal(10, result.Food.Id);
            Assert.Equal("Apple", result.Food.Name);

            // Verify changes in context
            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.Id == 1 && i.UserId == 42);
            Assert.NotNull(dbItem);
            Assert.Equal(1, dbItem.Id);
            Assert.Equal(10, dbItem.FoodId);
            Assert.True(dbItem.Purchased);
            Assert.Equal("updated", dbItem.Notes);
            Assert.NotNull(dbItem.Food);
            Assert.Equal(10, dbItem.Food.Id);
            Assert.Equal("Apple", dbItem.Food.Name);
        }

        [Fact]
        public async Task AddShoppingListItemAsync_ThrowsArgumentException_WhenRequestIsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddShoppingListItemAsync(null!, 42));
        }

        [Fact]
        public async Task AddShoppingListItemAsync_ThrowsArgumentException_WhenIdIsNotNull()
        {
            var context = CreateContext();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 1, FoodId = 10, Purchased = true, Notes = "note" };
            await Assert.ThrowsAsync<ArgumentException>(() => service.AddShoppingListItemAsync(request, 42));
        }

        [Fact]
        public async Task AddShoppingListItemAsync_ThrowsArgumentException_WhenFoodIdIsSetAndInvalid()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.SaveChanges();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { FoodId = 999, Purchased = true, Notes = "bad food" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.AddShoppingListItemAsync(request, 42));
        }

        [Fact]
        public async Task AddShoppingListItemAsync_AddsItem_WhenFoodIdIsSetAndValid()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 55, Name = "Bread" });
            context.SaveChanges();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { FoodId = 55, Purchased = false, Notes = "with food" };

            var result = await service.AddShoppingListItemAsync(request, 42);

            Assert.NotNull(result);
            Assert.Equal(55, result.FoodId);
            Assert.False(result.Purchased);
            Assert.Equal("with food", result.Notes);
            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.FoodId == 55 && i.UserId == 42);
            Assert.NotNull(dbItem);
            Assert.Equal(result.Id, dbItem.Id);
        }

        [Fact]
        public async Task AddShoppingListItemAsync_AddsItemSuccessfully()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 10, Name = "Apple" });
            context.SaveChanges();
            var service = CreateService(context);
            var request = new CreateUpdateShoppingListEntryRequestDto { FoodId = 10, Purchased = true, Notes = "new item" };
            var result = await service.AddShoppingListItemAsync(request, 42);
            Assert.NotNull(result);
            Assert.Equal(10, result.FoodId);
            Assert.True(result.Purchased);
            Assert.Equal("new item", result.Notes);
            // Verify item is in context
            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.FoodId == 10 && i.UserId == 42);
            Assert.NotNull(dbItem);
            Assert.Equal(result.Id, dbItem.Id);
        }

        [Fact]
        public async Task DeleteShoppingListItemAsync_ThrowsArgumentException_WhenIdIsInvalid()
        {
            var context = CreateContext();
            var service = CreateService(context);
            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteShoppingListItemAsync(0, 42));
        }

        [Fact]
        public async Task DeleteShoppingListItemAsync_ThrowsArgumentException_WhenItemNotFound()
        {
            var context = CreateContext();
            var service = CreateService(context);
            await Assert.ThrowsAsync<ArgumentException>(() => service.DeleteShoppingListItemAsync(99, 42));
        }

        [Fact]
        public async Task DeleteShoppingListItemAsync_DeletesItemSuccessfully()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 10, Name = "Apple" });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, UserId = 42, FoodId = 10, Purchased = false });
            context.SaveChanges();
            var service = CreateService(context);

            Assert.True(await service.DeleteShoppingListItemAsync(1, 42));

            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.Id == 1 && i.UserId == 42);
            Assert.Null(dbItem);

            //very that the underlying food is still there and didn't get cascade deleted
            var dbFood = context.Foods.FirstOrDefault(f => f.Id == 10);
            Assert.NotNull(dbFood);
        }

        [Fact]
        public async Task GenerateAsync_ThrowsArgumentException_WhenRequestIsNull()
        {
            var context = CreateContext();
            var service = CreateService(context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync(null!, 42));
        }

        [Fact]
        public async Task GenerateAsync_ThrowsArgumentException_WhenMealPlanIdIsInvalid()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 0, Restart = false };
            await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync(request, 42));
        }

        [Fact]
        public async Task GenerateAsync_ThrowsArgumentException_WhenUserCannotAccessMealPlan()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 1, Restart = false };
            context.MealPlans.Add(new MealPlan { Id = 1, UserId = 99 }); // Different user
            context.SaveChanges();

            await Assert.ThrowsAsync<ArgumentException>(() => service.GenerateAsync(request, 42));
        }

        [Fact]
        public async Task GenerateAsync_Succeeds_WhenValid()
        {
            var context = CreateContext();
            var service = CreateService(context);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 1, Restart = false };
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.MealPlans.Add(new MealPlan { Id = 1, UserId = 42 });
            context.SaveChanges();

            var ex = await Record.ExceptionAsync(() => service.GenerateAsync(request, 42));
            Assert.Null(ex);
        }

        [Fact]
        public async Task GenerateAsync_RestartTrue_ClearsExistingShoppingList()

        {
            var context = CreateContext();
            var service = CreateService(context);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 1, Restart = true };
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.MealPlans.Add(new MealPlan { Id = 1, UserId = 42 });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, Notes = "old list", UserId = 42 });
            context.SaveChanges();

            await service.GenerateAsync(request, 42);

            // Should have no shopping lists for this meal plan after restart
            Assert.Empty(context.ShoppingListItems.Where(sl => sl.Notes == "old list"));
        }

        [Fact]
        public async Task GenerateAsync_DoesNothing_WhenMealPlanHasNoMeals()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.MealPlans.Add(new MealPlan { Id = 2, UserId = 42, Meals = [] });
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 2, Restart = false };

            await service.GenerateAsync(request, 42);

            Assert.Empty(context.ShoppingListItems.Where(sl => sl.UserId == 42));
        }

        [Fact]
        public async Task GenerateAsync_DoesNothing_WhenMealsHaveNoRecipes()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var mealPlan = new MealPlan { Id = 3, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = null! }] };
            context.MealPlans.Add(mealPlan);
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 3, Restart = false };

            await service.GenerateAsync(request, 42);

            Assert.Empty(context.ShoppingListItems.Where(sl => sl.UserId == 42));
        }

        [Fact]
        public async Task GenerateAsync_DoesNothing_WhenRecipesHaveNoIngredients()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var recipe = new Recipe { Id = 1, UserId = 42, Ingredients = [], Instructions = "", Source = "", Title = "No Ingredients" };
            var mealPlan = new MealPlan { Id = 4, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = recipe }] };
            context.MealPlans.Add(mealPlan);
            context.Recipes.Add(recipe);
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 4, Restart = false };

            await service.GenerateAsync(request, 42);

            Assert.Empty(context.ShoppingListItems.Where(sl => sl.UserId == 42));
        }

        [Fact]
        public async Task GenerateAsync_DoesNothing_WhenAllFoodsInPantry()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var food = new Food { Id = 100, Name = "Egg" };
            var ingredient = new RecipeIngredient { Id = 1, FoodId = 100, Food = food, Quantity = 1 };
            var recipe = new Recipe { Id = 2, UserId = 42, Ingredients = [ingredient], Instructions = "", Source = "", Title = "Egg Recipe" };
            var mealPlan = new MealPlan { Id = 5, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = recipe }] };
            context.MealPlans.Add(mealPlan);
            context.Recipes.Add(recipe);
            context.Foods.Add(food);
            context.PantryItems.Add(new PantryItem { Id = 1, UserId = 42, FoodId = 100, Food = food, Quantity = 2 });
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 5, Restart = false };

            await service.GenerateAsync(request, 42);

            Assert.Empty(context.ShoppingListItems.Where(sl => sl.UserId == 42));
        }

        [Fact]
        public async Task GenerateAsync_AddsMissingFoodsToShoppingList()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var food = new Food { Id = 101, Name = "Milk" };
            var ingredient = new RecipeIngredient { Id = 2, FoodId = 101, Food = food, Quantity = 1 };
            var recipe = new Recipe { Id = 3, UserId = 42, Ingredients = [ingredient], Instructions = "", Source = "", Title = "Milk Recipe" };
            var mealPlan = new MealPlan { Id = 6, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = recipe }] };
            context.MealPlans.Add(mealPlan);
            context.Recipes.Add(recipe);
            context.Foods.Add(food);
            context.PantryItems.Add(new PantryItem { Id = 2, UserId = 42, FoodId = 999, Quantity = 2 }); // unrelated food
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 6, Restart = false };

            await service.GenerateAsync(request, 42);

            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.FoodId == 101 && i.UserId == 42);
            Assert.NotNull(dbItem);
        }

        [Fact]
        public async Task GenerateAsync_RestartTrue_RemovesOldAndAddsNewItems()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var food = new Food { Id = 102, Name = "Bread" };
            var ingredient = new RecipeIngredient { Id = 3, FoodId = 102, Food = food, Quantity = 1 };
            var recipe = new Recipe { Id = 4, UserId = 42, Ingredients = [ingredient], Instructions = "", Source = "", Title = "Bread Recipe" };
            var mealPlan = new MealPlan { Id = 7, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = recipe }] };
            context.MealPlans.Add(mealPlan);
            context.Recipes.Add(recipe);
            context.Foods.Add(food);
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 99, UserId = 42, FoodId = 999, Notes = "old" });
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 7, Restart = true };

            await service.GenerateAsync(request, 42);

            Assert.Null(context.ShoppingListItems.FirstOrDefault(i => i.FoodId == 999 && i.UserId == 42));
            var dbItem = context.ShoppingListItems.FirstOrDefault(i => i.FoodId == 102 && i.UserId == 42);
            Assert.NotNull(dbItem);
        }

        [Fact]
        public async Task GenerateAsync_IgnoresIngredientWithNullFood()
        {
            var context = CreateContext();
            var service = CreateService(context);
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            var ingredient = new RecipeIngredient { Id = 4, FoodId = 103, Food = null!, Quantity = 1 };
            var recipe = new Recipe { Id = 5, UserId = 42, Ingredients = [ingredient], Instructions = "", Source = "", Title = "Null Food Recipe" };
            var mealPlan = new MealPlan { Id = 8, UserId = 42, Meals = [new MealPlanEntry { Id = 1, Recipe = recipe }] };
            context.MealPlans.Add(mealPlan);
            context.Recipes.Add(recipe);
            context.SaveChanges();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 8, Restart = false };

            await service.GenerateAsync(request, 42);

            Assert.Empty(context.ShoppingListItems.Where(sl => sl.UserId == 42));
        }
    }
}