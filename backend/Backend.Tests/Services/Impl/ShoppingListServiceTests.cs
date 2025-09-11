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