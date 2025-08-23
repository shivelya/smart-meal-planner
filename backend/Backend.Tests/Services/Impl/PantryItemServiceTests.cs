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
            var dto = new CreatePantryItemDto { IngredientId = 1, Quantity = 2, Unit = "kg" };
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
                new CreatePantryItemDto { IngredientId = 1, Quantity = 2, Unit = "kg" },
                new CreatePantryItemDto { IngredientId = 2, Quantity = 3, Unit = "g" }
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

        [Fact]
        public async Task UpdatePantryItemAsync_ThrowsNotImplementedException()
        {
            // Arrange
            var dto = new PantryItemDto();

            // Act & Assert
            await Assert.ThrowsAsync<System.NotImplementedException>(() => _service.UpdatePantryItemAsync(dto));
        }
    }
}
