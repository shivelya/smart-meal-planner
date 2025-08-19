using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class CategoryDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new CategoryDto { Id = 1, Name = "Produce" };
            Assert.Equal(1, dto.Id);
            Assert.Equal("Produce", dto.Name);
        }
    }
}
