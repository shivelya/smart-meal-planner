using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class RecipeIngredientDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new RecipeIngredientDto { Quantity = 1.5m, Unit = "cup",
                Ingredient = new IngredientDto{ Name = "name", Id = 1, Category = new CategoryDto()} };
            Assert.Equal(1.5m, dto.Quantity);
            Assert.Equal("cup", dto.Unit);
        }
    }
}
