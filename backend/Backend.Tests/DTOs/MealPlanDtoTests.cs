using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class MealPlanDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var now = DateTime.Now;
            var dto = new MealPlanDto { Id = 5, StartDate = now, Meals = [] };
            Assert.Equal(5, dto.Id);
            Assert.Equal(now, dto.StartDate);
        }
    }
}
