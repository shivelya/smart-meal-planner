using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class MealPlanDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var now = DateTime.Now;
            var dto = new MealPlanDto { Id = 5, UserId = 1, StartDate = now };
            Assert.Equal(5, dto.Id);
            Assert.Equal(1, dto.UserId);
            Assert.Equal(now, dto.StartDate);
        }
    }
}
