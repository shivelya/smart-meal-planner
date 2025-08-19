using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Tests.DTOs
{
    public class PantryItemDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new PantryItemDto { Id = 3, UserId = 1, IngredientId = 2, Quantity = 5.5m, Unit = "kg" };
            Assert.Equal(3, dto.Id);
            Assert.Equal(1, dto.UserId);
            Assert.Equal(2, dto.IngredientId);
            Assert.Equal(5.5m, dto.Quantity);
            Assert.Equal("kg", dto.Unit);
        }
    }
}
