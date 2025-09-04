using Backend.DTOs;
using Org.BouncyCastle.Asn1.Misc;

namespace Backend.Tests.DTOs
{
    public class RecipeIngredientDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new RecipeIngredientDto { Quantity = 1.5m, Unit = "cup",
                Food = new FoodDto{ Name = "name", Id = 1, Category = new CategoryDto { Id = 1, Name = "produce "}} };
            Assert.Equal(1.5m, dto.Quantity);
            Assert.Equal("cup", dto.Unit);
        }

        [Fact]
        public void RecipeIngredientRequestDto_PropertyTest()
        {
            var dto = new RecipeIngredientDto { Quantity = 3.3m, Unit = "oz", Id = 7, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce" }} };
            Assert.Equal(3.3m, dto.Quantity);
            Assert.Equal("oz", dto.Unit);
        }

        [Fact]
        public void CreateRecipeIngredientNewIngredientDto_PropertyTest()
        {
            var dto = new RecipeIngredientDto { Quantity = 4.4m, Unit = "ml", Food = new FoodDto { Id = 1, Category = new CategoryDto { Id = 1, Name = "produce" }, Name = "Egg" }, Id = 7 };
            Assert.Equal(4.4m, dto.Quantity);
            Assert.Equal("ml", dto.Unit);
            Assert.Equal("Egg", dto.Food.Name);
            Assert.Equal(7, dto.Id);
        }
    }
}
