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

        [Fact]
        public void CreatePantryItemDto_PropertyTest()
        {
            var dto = new TestCreatePantryItemDto { Id = 7, Quantity = 2.5m, Unit = "g" };
            Assert.Equal(7, dto.Id);
            Assert.Equal(2.5m, dto.Quantity);
            Assert.Equal("g", dto.Unit);
        }

        private class TestCreatePantryItemDto : CreatePantryItemDto { }

        [Fact]
        public void CreatePantryItemNewIngredientDto_PropertyTest()
        {
            var dto = new CreatePantryItemNewIngredientDto { Id = 8, Quantity = 1.1m, Unit = "oz", IngredientName = "Salt", CategoryId = 2 };
            Assert.Equal(8, dto.Id);
            Assert.Equal(1.1m, dto.Quantity);
            Assert.Equal("oz", dto.Unit);
            Assert.Equal("Salt", dto.IngredientName);
            Assert.Equal(2, dto.CategoryId);
        }

        [Fact]
        public void CreatePantryItemOldIngredientDto_PropertyTest()
        {
            var dto = new CreatePantryItemOldIngredientDto { Id = 9, Quantity = 3.3m, Unit = "lb", IngredientId = 5 };
            Assert.Equal(9, dto.Id);
            Assert.Equal(3.3m, dto.Quantity);
            Assert.Equal("lb", dto.Unit);
            Assert.Equal(5, dto.IngredientId);
        }

        [Fact]
        public void GetPantryItemsResult_PropertyTest()
        {
            var items = new List<PantryItemDto> { new PantryItemDto { Id = 1, UserId = 2, Ingredient = new IngredientDto { Category = new CategoryDto() }, Quantity = 1, Unit = "g" } };
            var dto = new GetPantryItemsResult { TotalCount = 1, Items = items };
            Assert.Equal(1, dto.TotalCount);
            Assert.Equal(items, dto.Items);
        }
    }
}
