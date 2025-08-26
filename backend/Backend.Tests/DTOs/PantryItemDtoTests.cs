using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class PantryItemDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var ing = new IngredientDto { Category = new CategoryDto() };
            var dto = new PantryItemDto { Id = 3, UserId = 1, Ingredient = ing, Quantity = 5.5m, Unit = "kg" };
            Assert.Equal(3, dto.Id);
            Assert.Equal(1, dto.UserId);
            Assert.Equal(ing, dto.Ingredient);
            Assert.Equal(5.5m, dto.Quantity);
            Assert.Equal("kg", dto.Unit);
        }
    }
}
