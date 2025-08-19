using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class RecipeIngredientDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new RecipeIngredientDto { RecipeId = 4, IngredientId = 2, Quantity = 1.5m, Unit = "cup" };
            Assert.Equal(4, dto.RecipeId);
            Assert.Equal(2, dto.IngredientId);
            Assert.Equal(1.5m, dto.Quantity);
            Assert.Equal("cup", dto.Unit);
        }
    }
}
