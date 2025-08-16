using Xunit;
using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Tests.DTOs
{
    public class MealPlanEntryDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var now = DateTime.Now;
            var dto = new MealPlanEntryDto { Id = 6, MealPlanId = 5, RecipeId = 4, Date = now };
            Assert.Equal(6, dto.Id);
            Assert.Equal(5, dto.MealPlanId);
            Assert.Equal(4, dto.RecipeId);
            Assert.Equal(now, dto.Date);
        }
    }
}
