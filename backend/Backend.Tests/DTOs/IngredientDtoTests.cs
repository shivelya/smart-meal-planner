using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class IngredientDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new FoodReferenceDto { Id = 2, Name = "Tomato", Category = new CategoryDto() };
            Assert.Equal(2, dto.Id);
            Assert.Equal("Tomato", dto.Name);
        }
    }
}
