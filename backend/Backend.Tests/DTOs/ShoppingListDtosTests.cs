using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class GenerateShoppingListRequestDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new GenerateShoppingListRequestDto { MealPlanId = 123, Restart = true };
            Assert.Equal(123, dto.MealPlanId);
            Assert.True(dto.Restart);
        }
    }

    public class GetShoppingListResultTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var foods = new List<ShoppingListItemDto> {
                new() { Id = 1, FoodId = 2, Purchased = false },
                new() { Id = 2, FoodId = 3, Purchased = true }
            };
            var dto = new GetShoppingListResult { TotalCount = 2, Foods = foods };
            Assert.Equal(2, dto.TotalCount);
            Assert.Equal(foods, dto.Foods);
        }
    }

    public class ShoppingListItemDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var foodDto = new FoodDto { Id = 5, Name = "Apple", Category = new CategoryDto { Id = 1, Name = "Fruits" } };
            var dto = new ShoppingListItemDto {
                Id = 10,
                FoodId = 5,
                Food = foodDto,
                Purchased = true,
                Notes = "Organic"
            };
            Assert.Equal(10, dto.Id);
            Assert.Equal(5, dto.FoodId);
            Assert.Equal(foodDto, dto.Food);
            Assert.True(dto.Purchased);
            Assert.Equal("Organic", dto.Notes);
        }

        [Fact]
        public void CanSetNullProperties()
        {
            var dto = new ShoppingListItemDto {
                Id = 1,
                FoodId = 2,
                Food = null,
                Purchased = false,
                Notes = null
            };
            Assert.Null(dto.Food);
            Assert.Null(dto.Notes);
        }
    }
}
