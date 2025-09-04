using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class PantryItemDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var ing = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} };
            var dto = new PantryItemDto { Id = 3, Food = ing, Quantity = 5.5m, Unit = "kg" };
            Assert.Equal(3, dto.Id);
            Assert.Equal(ing, dto.Food);
            Assert.Equal(5.5m, dto.Quantity);
            Assert.Equal("kg", dto.Unit);
        }

        [Fact]
        public void CreatePantryItemDto_PropertyTest()
        {
            var dto = new PantryItemDto { Id = 7, Quantity = 2.5m, Unit = "g", Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce"}} };
            Assert.Equal(7, dto.Id);
            Assert.Equal(2.5m, dto.Quantity);
            Assert.Equal("g", dto.Unit);
        }

        [Fact]
        public void CreatePantryItemNewIngredientDto_PropertyTest()
        {
            var dto = new PantryItemDto
            {
                Id = 8,
                Quantity = 1.1m,
                Unit = "oz",
                Food = new FoodDto { Id = 1, Category = new CategoryDto { Id = 1, Name = "produce"}, Name = "Salt"  }
            };
            Assert.Equal(8, dto.Id);
            Assert.Equal(1.1m, dto.Quantity);
            Assert.Equal("oz", dto.Unit);
            Assert.Equal("Salt", dto.Food.Name);
        }

        [Fact]
        public void CreatePantryItemOldIngredientDto_PropertyTest()
        {
            var dto = new PantryItemDto
            {
                Id = 9,
                Quantity = 3.3m,
                Unit = "lb",
                Food = new FoodDto { Id = 5, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce"} }
            };
            Assert.Equal(9, dto.Id);
            Assert.Equal(3.3m, dto.Quantity);
            Assert.Equal("lb", dto.Unit);
            Assert.Equal(5, dto.Food.Id);
        }

        [Fact]
        public void GetPantryItemsResult_PropertyTest()
        {
            var items = new List<PantryItemDto> { new PantryItemDto { Id = 1, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 1, Unit = "g" } };
            var dto = new GetPantryItemsResult { TotalCount = 1, Items = items };
            Assert.Equal(1, dto.TotalCount);
            Assert.Equal(items, dto.Items);
        }
    }
}
