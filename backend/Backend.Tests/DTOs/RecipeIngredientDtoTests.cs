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

        [Fact]
        public void CreateRecipeIngredientDto_PropertyTest()
        {
            var dto = new TestCreateRecipeIngredientDto { Quantity = 2.2m, Unit = "g" };
            Assert.Equal(2.2m, dto.Quantity);
            Assert.Equal("g", dto.Unit);
        }

        private class TestCreateRecipeIngredientDto : CreateRecipeIngredientDto { }

        [Fact]
        public void CreateRecipeIngredientOldIngredientDto_PropertyTest()
        {
            var dto = new CreateRecipeIngredientOldIngredientDto { Quantity = 3.3m, Unit = "oz", IngredientId = 7 };
            Assert.Equal(3.3m, dto.Quantity);
            Assert.Equal("oz", dto.Unit);
            Assert.Equal(7, dto.IngredientId);
        }

        [Fact]
        public void CreateRecipeIngredientNewIngredientDto_PropertyTest()
        {
            var dto = new CreateRecipeIngredientNewIngredientDto { Quantity = 4.4m, Unit = "ml", IngredientName = "Egg", CategoryId = 2 };
            Assert.Equal(4.4m, dto.Quantity);
            Assert.Equal("ml", dto.Unit);
            Assert.Equal("Egg", dto.IngredientName);
            Assert.Equal(2, dto.CategoryId);
        }
    }
}
