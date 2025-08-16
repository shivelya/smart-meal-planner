using Xunit;
using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Tests.DTOs
{
    public class IngredientDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new IngredientDto { Id = 2, Name = "Tomato", CategoryId = 1 };
            Assert.Equal(2, dto.Id);
            Assert.Equal("Tomato", dto.Name);
            Assert.Equal(1, dto.CategoryId);
        }
    }
}
