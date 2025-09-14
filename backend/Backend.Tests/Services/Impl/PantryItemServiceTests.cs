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
            var food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 };
            var dto = new CreateUpdatePantryItemRequestDto { Food = food, Quantity = 2, Unit = "kg" };
            plannerContext.Foods.Add(new Food { Id = 1, Name = "juice", Category = new Category { Name = "refrigerated" } });
            plannerContext.SaveChanges();
            var userId = 42;

            // Act
            var result = await _service.CreatePantryItemAsync(dto, userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(food.Id, result.Food.Id);
            Assert.Equal(dto.Quantity, result.Quantity);
            Assert.Equal(dto.Unit, result.Unit);
        }

        [Fact]
        public async Task CreatePantryItemsAsync_CreatesMultipleItems_ReturnsDtos()
        {
            // Arrange
            var dtos = new List<CreateUpdatePantryItemRequestDto>
            {
                new() { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" },
                new() { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 2 }, Quantity = 3, Unit = "g" }
            };
            var userId = 42;
            plannerContext.Foods.Add(new Food { Id = 1, Name = "banana", Category = new Category { Name = "produce" } });
            plannerContext.Foods.Add(new Food { Id = 2, Name = "apple", Category = new Category { Name = "produce" } });
            plannerContext.SaveChanges();

            // Act
            var result = await _service.CreatePantryItemsAsync(dtos, userId);

            // Assert
            Assert.Equal(2, result.TotalCount);
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
            var items = new List<PantryItem> { new() { Id = 1 }, new() { Id = 2 } };
            plannerContext.PantryItems.AddRange(items);
            plannerContext.SaveChanges();

            // Act
            var result = await _service.DeletePantryItemsAsync(ids);

            // Assert
            Assert.Equal(2, result.Ids.Count());
        }

        [Fact]
        public async Task GetPantryItemByIdAsync_ItemExists_ReturnsDto()
        {
            // Arrange
            var item = new PantryItem
            {
                Id = 1,
                FoodId = 2,
                Quantity = 3,
                Unit = "g",
                UserId = 42,
                Food = new Food { Id = 1, Name = "banana", Category = new Category { Name = "produce" } }
            };
            plannerContext.PantryItems.Add(item);
            plannerContext.SaveChanges();

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

            // Seed foods
            var category = new Category { Name = "produce" };
            var food1 = new Food { Id = 1, Name = "Salt", CategoryId = 1, Category = category };
            var food2 = new Food { Id = 2, Name = "Sugar", CategoryId = 1, Category = category };
            var food3 = new Food { Id = 3, Name = "Pepper", CategoryId = 2, Category = category };
            context.Foods.AddRange(food1, food2, food3);

            // Seed pantry items
            context.PantryItems.AddRange(
                new PantryItem { Id = 1, FoodId = 1, Quantity = 2, Unit = "g", UserId = userId, Food = food1 },
                new PantryItem { Id = 2, FoodId = 2, Quantity = 5, Unit = "g", UserId = userId, Food = food2 },
                new PantryItem { Id = 3, FoodId = 3, Quantity = 1, Unit = "g", UserId = userId, Food = food3 },
                new PantryItem { Id = 4, FoodId = 2, Quantity = 3, Unit = "g", UserId = 99, Food = food2 } // different user
            );
            context.SaveChanges();

            return new PantryItemService(context, logger);
        }

        [Fact]
        public async Task Search_ReturnsMatchingItems_ForUser()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("salt", userId, null, null);

            Assert.Single(results.Items);
            Assert.Equal(1, results.Items.First().Food.Id);
        }

        [Fact]
        public async Task Search_ReturnsMultipleMatches()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("s", userId, null, null); // matches "Sugar" and "Salt"

            Assert.Equal(2, results.TotalCount);
            var names = results.Items.Select(r => r.Food.Id).ToList();
            Assert.Contains(1, names);
            Assert.Contains(2, names);
        }

        [Fact]
        public async Task Search_IsCaseInsensitive()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("SALT", userId, null, null);

            Assert.Single(results.Items);
            Assert.Equal(1, results.Items.First().Food.Id);
        }

        [Fact]
        public async Task Search_ReturnsEmpty_WhenNoMatch()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("flour", userId, null, null);

            Assert.Empty(results.Items);
        }

        [Fact]
        public async Task Search_OnlyReturnsItemsForSpecifiedUser()
        {
            var service = CreateServiceWithData(out int userId);

            var results = await service.Search("Sugar", userId, null, null);

            Assert.Single(results.Items);
            Assert.Equal(2, results.Items.First().Id);  // should return 2 and not 4
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

            var food = new Food { Id = 1, Name = "Salt", CategoryId = 1, Category = new Category { Name = "produce" } };
            context.Foods.Add(food);

            for (int i = 0; i < 25; i++)
            {
                context.PantryItems.Add(new PantryItem
                {
                    Id = i + 1,
                    FoodId = 1,
                    Quantity = 1,
                    Unit = "g",
                    UserId = userId,
                    Food = food
                });
            }
            context.SaveChanges();

            var service = new PantryItemService(context, logger);

            var results = await service.Search("Salt", userId, 20, null);

            Assert.Equal(25, results.TotalCount);
            Assert.Equal(20, results.Items.Count());
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

            var food = new Food { Id = 1, Name = "Salt", CategoryId = 1, Category = new Category { Name = "produce" } };
            context.Foods.Add(food);

            var pantryItem = new PantryItem { Id = 10, FoodId = 1, Quantity = 2, Unit = "g", UserId = userId, Food = food };
            context.PantryItems.Add(pantryItem);

            context.SaveChanges();

            return new PantryItemService(context, logger);
        }

        [Fact]
        public async Task UpdatePantryItemAsync_UpdatesQuantityAndUnit()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreateUpdatePantryItemRequestDto { Id = 10, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 5, Unit = "kg" };

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
            var dto = new CreateUpdatePantryItemRequestDto { Id = 10, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, 999));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfIdIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfPantryItemNotFound()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var dto = new CreateUpdatePantryItemRequestDto { Id = 999, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsIfUserDoesNotOwnItem()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var otherUser = new User { Id = 2, Email = "other@example.com", PasswordHash = Guid.NewGuid().ToString() };
            context.Users.Add(otherUser);
            var pantryItem = new PantryItem { Id = 20, FoodId = 1, Quantity = 2, Unit = "g", UserId = 2 };
            context.PantryItems.Add(pantryItem);
            context.SaveChanges();

            var dto = new CreateUpdatePantryItemRequestDto { Id = 20, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 5, Unit = "kg" };

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_UpdatesFoodIdIfProvided()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var newFood = new Food { Id = 2, Name = "Sugar", CategoryId = 1 };
            context.Foods.Add(newFood);
            context.SaveChanges();

            var dto = new CreateUpdatePantryItemRequestDto { Id = 10, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 2 }, Quantity = 5, Unit = "kg" };

            var result = await service.UpdatePantryItemAsync(dto, userId);

            Assert.Equal(2, result.Food.Id);
        }

        [Fact]
        public async Task CreatePantryItemsAsync_ReturnsEmpty_WhenFoodIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var dtos = new[] { new CreateUpdatePantryItemRequestDto { Food = null!, Quantity = 1 } };
            var result = await service.CreatePantryItemsAsync(dtos, 1);
            Assert.Empty(result.Items);
            Assert.Equal(0, result.TotalCount);
        }

        [Fact]
        public async Task UpdatePantryItemAsync_Throws_WhenDtoIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(null!, 1));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_Throws_WhenIdIsNull()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = null, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, 1));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_Throws_WhenItemNotFound()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = 999, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, 1));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_Throws_WhenUserIdMismatch()
        {
            var service = CreateServiceWithData(out int userId, out var context);
            var item = new PantryItem { Id = 1, UserId = 2, FoodId = 1, Quantity = 1, Unit = "g" };
            context.PantryItems.Add(item);
            context.SaveChanges();

            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = 1, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 } };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(dto, 1));
        }

        [Fact]
        public async Task UpdatePantryItemFood_Throws_WhenFoodIdInvalid()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 999 } };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = 1, Food = dto.Food }, 1));
        }

        [Fact]
        public async Task UpdatePantryItemFood_Throws_WhenCategoryIdInvalid()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, CategoryId = 999, Name = "Test" } };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = 1, Food = dto.Food }, 1));
        }

        [Fact]
        public async Task UpdatePantryItemFood_Throws_WhenModeIsUnknown()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            var food = new ExistingFoodReferenceDto
            {
                Id = 1,
                Mode = (AddFoodMode)2
            };
            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Food = food };
            await Assert.ThrowsAsync<ArgumentException>(() => service.UpdatePantryItemAsync(new CreateUpdatePantryItemRequestDto { Quantity = 1, Id = 1, Food = dto.Food }, 1));
        }

        [Fact]
        public async Task Search_Throws_WhenSkipIsNegative()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.Search("test", 1, null, -1));
        }

        [Fact]
        public async Task Search_Throws_WhenTakeIsNegative()
        {
            var service = CreateServiceWithData(out int userId, out var context);

            await Assert.ThrowsAsync<ArgumentException>(() => service.Search("test", 1, -1, null));
        }

        [Fact]
        public async Task CreatePantryItemAsync_Throws_WhenQuantityIsNegative()
        {
            // Arrange
            var food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 };
            var dto = new CreateUpdatePantryItemRequestDto { Food = food, Quantity = -5, Unit = "kg" };
            plannerContext.Foods.Add(new Food { Id = 1, Name = "juice", Category = new Category { Name = "refrigerated" } });
            plannerContext.SaveChanges();
            var userId = 42;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.CreatePantryItemAsync(dto, userId));
        }

        [Fact]
        public async Task UpdatePantryItemAsync_Throws_WhenQuantityIsNegative()
        {
            // Arrange
            var food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 };
            var pantryItem = new PantryItem { Id = 1, FoodId = 1, Quantity = 2, Unit = "kg", UserId = 42 };
            plannerContext.Foods.Add(new Food { Id = 1, Name = "juice", Category = new Category { Name = "refrigerated" } });
            plannerContext.PantryItems.Add(pantryItem);
            plannerContext.SaveChanges();
            var dto = new CreateUpdatePantryItemRequestDto { Id = 1, Food = food, Quantity = -10, Unit = "kg" };
            var userId = 42;

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdatePantryItemAsync(dto, userId));
        }
    }
}
