using Backend.DTOs;

namespace Backend.Tests.DTOs
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
