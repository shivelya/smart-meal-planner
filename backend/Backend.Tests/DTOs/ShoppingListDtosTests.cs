using Backend.DTOs;
using Backend.Model;

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
            public void MappingExtensions_ToDto_MapsEntityToDtoCorrectly()
            {
                var category = new Category { Id = 1, Name = "Fruit" };
                var food = new Food { Id = 5, Name = "Apple", Category = category };
                var entity = new ShoppingListItem
                {
                    Id = 10,
                    FoodId = 5,
                    Food = food,
                    Purchased = true,
                    Notes = "Organic"
                };

                var dto = entity.ToDto();
                Assert.Equal(entity.Id, dto.Id);
                Assert.Equal(entity.FoodId, dto.FoodId);
                Assert.Equal(entity.Purchased, dto.Purchased);
                Assert.Equal(entity.Notes, dto.Notes);
                Assert.NotNull(dto.Food);
                Assert.Equal(food.Id, dto.Food.Id);
                Assert.Equal(food.Name, dto.Food.Name);
                Assert.NotNull(dto.Food.Category);
                Assert.Equal(category.Id, dto.Food.Category.Id);
                Assert.Equal(category.Name, dto.Food.Category.Name);
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
