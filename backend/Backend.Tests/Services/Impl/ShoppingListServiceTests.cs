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
        }

        [Fact]
        public void GetShoppingList_ReturnsEmpty_WhenNoItemsExist()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.SaveChanges();
            var service = CreateService(context);

            var result = service.GetShoppingList(42);
            Assert.NotNull(result);
            Assert.Equal(0, result.TotalCount);
            Assert.Empty(result.Foods);
        }

        [Fact]
        public void GetShoppingList_ReturnsItems_ForUser()
        {
            var context = CreateContext();
            context.Users.Add(new User { Id = 42, Email = "user@example.com", PasswordHash = "hash" });
            context.Foods.Add(new Food { Id = 10, Name = "Apple" });
            context.Foods.Add(new Food { Id = 20, Name = "Banana" });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 1, UserId = 42, FoodId = 10, Purchased = false });
            context.ShoppingListItems.Add(new ShoppingListItem { Id = 2, UserId = 42, FoodId = 20, Purchased = true });
            context.SaveChanges();
            var service = CreateService(context);

            var result = service.GetShoppingList(42);
            Assert.NotNull(result);
            Assert.Equal(2, result.TotalCount);
            Assert.Collection(result.Foods,
                item => Assert.Equal(1, item.Id),
                item => Assert.Equal(2, item.Id));
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

            await Assert.ThrowsAsync<ValidationException>(() => service.GenerateAsync(request, 42));
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
    }
}