using Backend.DTOs;
using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class PantryItemServiceTests
    {
        private readonly Mock<ILogger<PantryItemService>> _loggerMock;
        private readonly PlannerContext plannerContext;
        private readonly PantryItemService _service;

        public PantryItemServiceTests()
        {
            _loggerMock = new Mock<ILogger<PantryItemService>>();
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()).Options;
            var logger = new Mock<ILogger<PlannerContext>>();
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            plannerContext = new PlannerContext(options, config, logger.Object);
            _service = new PantryItemService(plannerContext, _loggerMock.Object);
        }

        [Fact]
        public async Task CreatePantryItemAsync_CreatesItem_ReturnsDto()
        {
            // Arrange
            var dto = new CreatePantryItemOldIngredientDto { IngredientId = 1, Quantity = 2, Unit = "kg" };
            plannerContext.Ingredients.Add(new Ingredient { Id = 1, Name = "juice" });
            plannerContext.SaveChanges();
            var userId = 42;

            // Act
            var result = await _service.CreatePantryItemAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.IngredientId, result.IngredientId);
            Assert.Equal(dto.Quantity, result.Quantity);
            Assert.Equal(dto.Unit, result.Unit);
            Assert.Equal(userId, result.UserId);
        }

        [Fact]
        public async Task CreatePantryItemsAsync_CreatesMultipleItems_ReturnsDtos()
        {
            // Arrange
            var dtos = new List<CreatePantryItemDto>
            {
                new CreatePantryItemOldIngredientDto { IngredientId = 1, Quantity = 2, Unit = "kg" },
                new CreatePantryItemOldIngredientDto { IngredientId = 2, Quantity = 3, Unit = "g" }
            };
            var userId = 42;

            // Act
            var result = await _service.CreatePantryItemsAsync(dtos, userId);

            // Assert
            Assert.Equal(2, result.Count());
            Assert.All(result, r => Assert.Equal(userId, r.UserId));
        }

        [Fact]
        public async Task DeletePantryItemAsync_ItemExists_DeletesAndReturnsTrue()
        {
            // Arrange
            var item = new PantryItem { Id = 1 };
            plannerContext.PantryItems.Add(item);
            plannerContext.SaveChanges();

            // Act
            var result = await _service.DeletePantryItemAsync(1);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task DeletePantryItemAsync_ItemDoesNotExist_ReturnsFalse()
        {
            // Act
            var result = await _service.DeletePantryItemAsync(1);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task DeletePantryitemsAsync_DeletesMultipleItems_ReturnsCount()
        {
            // Arrange
            var ids = new List<int> { 1, 2 };
            var items = new List<PantryItem> { new PantryItem { Id = 1 }, new PantryItem { Id = 2 } };
            plannerContext.PantryItems.AddRange(items);

            // Act
            var result = await _service.DeletePantryItemsAsync(ids);

            // Assert
            Assert.Equal(2, result);
        }

        [Fact]
        public async Task GetPantryItemByIdAsync_ItemExists_ReturnsDto()
        {
            // Arrange
            var item = new PantryItem { Id = 1, IngredientId = 2, Quantity = 3, Unit = "g", UserId = 42 };
            plannerContext.PantryItems.Add(item);

            // Act
            var result = await _service.GetPantryItemByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(item.Id, result.Id);
        }

        [Fact]
        public async Task GetPantryItemByIdAsync_ItemDoesNotExist_ReturnsNull()
        {
            // Act
            var result = await _service.GetPantryItemByIdAsync(1);

            // Assert
            Assert.Null(result);
        }

        private PantryItemService CreateServiceWithData(out int userId)
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var logger = new LoggerFactory().CreateLogger<PantryItemService>();
            var context = new PlannerContext(options, null!, new LoggerFactory().CreateLogger<PlannerContext>());
            userId = 1;

            // Seed ingredients
            var ingredient1 = new Ingredient { Id = 1, Name = "Salt", CategoryId = 1 };
            var ingredient2 = new Ingredient { Id = 2, Name = "Sugar", CategoryId = 1 };
            var ingredient3 = new Ingredient { Id = 3, Name = "Pepper", CategoryId = 2 };
            context.Ingredients.AddRange(ingredient1, ingredient2, ingredient3);

            // Seed pantry items
            context.PantryItems.AddRange(
                new PantryItem { Id = 1, IngredientId = 1, Quantity = 2, Unit = "g", UserId = userId, Ingredient = ingredient1 },
                new PantryItem { Id = 2, IngredientId = 2, Quantity = 5, Unit = "g", UserId = userId, Ingredient = ingredient2 },
                new PantryItem { Id = 3, IngredientId = 3, Quantity = 1, Unit = "g", UserId = userId, Ingredient = ingredient3 },
                new PantryItem { Id = 4, IngredientId = 2, Quantity = 3, Unit = "g", UserId = 99, Ingredient = ingredient2 } // different user
            );
            context.SaveChanges();

            return new PantryItemService(context, logger);
        }

        [Fact]
        public async Task Search_ReturnsMatchingItems_ForUser()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("salt", userId);

            Assert.Single(results);
            Assert.Equal(1, results.First().IngredientId);
        }

        [Fact]
        public async Task Search_ReturnsMultipleMatches()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("s", userId); // matches "Sugar" and "Salt"

            Assert.Equal(2, results.Count());
            var names = results.Select(r => r.IngredientId).ToList();
            Assert.Contains(1, names);
            Assert.Contains(2, names);
        }

        [Fact]
        public async Task Search_IsCaseInsensitive()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("SALT", userId);

            Assert.Single(results);
            Assert.Equal(1, results.First().IngredientId);
        }

        [Fact]
        public async Task Search_ReturnsEmpty_WhenNoMatch()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("flour", userId);

            Assert.Empty(results);
        }

        [Fact]
        public async Task Search_OnlyReturnsItemsForSpecifiedUser()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("Sugar", userId);

            Assert.Single(results);
            Assert.Equal(userId, results.First().UserId);
        }

        [Fact]
        public async Task Search_LimitsResultsTo20()
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var logger = new LoggerFactory().CreateLogger<PantryItemService>();
            var context = new PlannerContext(options, null!, new LoggerFactory().CreateLogger<PlannerContext>());
            int userId = 1;

            var ingredient = new Ingredient { Id = 1, Name = "Salt", CategoryId = 1 };
            context.Ingredients.Add(ingredient);

            for (int i = 0; i < 25; i++)
            {
                context.PantryItems.Add(new PantryItem
                {
                    Id = i + 1,
                    IngredientId = 1,
                    Quantity = 1,
                    Unit = "g",
                    UserId = userId,
                    Ingredient = ingredient
                });
            }
            context.SaveChanges();

            var service = new PantryItemService(context, logger);

            var results = await service.Search("Salt", userId);

            Assert.Equal(20, results.Count());
        }

        private PantryItemService CreateServiceWithData(out int userId, out PlannerContext context)
        {
            var options = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var logger = new LoggerFactory().CreateLogger<PantryItemService>();
            context = new PlannerContext(options, null!, new LoggerFactory().CreateLogger<PlannerContext>());
            userId = 1;

            var user = new User { Id = userId, Email = "test@example.com", PasswordHash = Guid.NewGuid().ToString() };
            context.Users.Add(user);

            var ingredient = new Ingredient { Id = 1, Name = "Salt", CategoryId = 1 };
            context.Ingredients.Add(ingredient);

            var pantryItem = new PantryItem { Id = 10, IngredientId = 1, Quantity = 2, Unit = "g", UserId = userId, Ingredient = ingredient };
            context.PantryItems.Add(pantryItem);

            context.SaveChanges();

            return new PantryItemService(context, logger);
        }

        [Fact]
        public async Task UpdatePantryItemAsync_UpdatesQuantityAndUnit()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreatePantryItemOldIngredientDto { Id = 10, IngredientId = 1, Quantity = 5, Unit = "kg" };

            var result = await service.UpdatePantryItemAsync(dto, userId);

            Assert.Equal(10, result.Id);
            Assert.Equal(5, result.Quantity);
            Assert.Equal("kg", result.Unit);
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfDtoIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(null!, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfUserNotFound()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreatePantryItemOldIngredientDto { Id = 10, IngredientId = 1, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, 999));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfIdIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreatePantryItemOldIngredientDto { IngredientId = 1, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfPantryItemNotFound()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreatePantryItemOldIngredientDto { Id = 999, IngredientId = 1, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfUserDoesNotOwnItem()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var otherUser = new User { Id = 2, Email = "other@example.com", PasswordHash = Guid.NewGuid().ToString() };
            context.Users.Add(otherUser);
            var pantryItem = new PantryItem { Id = 20, IngredientId = 1, Quantity = 2, Unit = "g", UserId = 2 };
            context.PantryItems.Add(pantryItem);
            context.SaveChanges();

            var dto = new CreatePantryItemOldIngredientDto { Id = 20, IngredientId = 1, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_UpdatesIngredientIdIfProvided()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var newIngredient = new Ingredient { Id = 2, Name = "Sugar", CategoryId = 1 };
            context.Ingredients.Add(newIngredient);
            context.SaveChanges();

            var dto = new CreatePantryItemOldIngredientDto { Id = 10, IngredientId = 2, Quantity = 5, Unit = "kg" };

            var result = await service.UpdatePantryItemAsync(dto, userId);

            Assert.Equal(2, result.IngredientId);
        }
    }
}
