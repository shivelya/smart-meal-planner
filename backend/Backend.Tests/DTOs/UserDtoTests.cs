using Xunit;
using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Tests.DTOs
{
    public class UserDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new UserDto { Id = 1, Email = "test@example.com" };
            Assert.Equal(1, dto.Id);
            Assert.Equal("test@example.com", dto.Email);
        }
    }
}
