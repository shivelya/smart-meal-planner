using Backend.DTOs;

namespace Backend.Tests.DTOs
{
    public class RecipeDtoTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var dto = new RecipeDto {
                Id = 4,
                UserId = 1,
                Title = "Pasta",
                Source = "Cookbook",
                Instructions = "Boil water.",
                ImageURL = "http://image.url"
            };
            Assert.Equal(4, dto.Id);
            Assert.Equal(1, dto.UserId);
            Assert.Equal("Pasta", dto.Title);
            Assert.Equal("Cookbook", dto.Source);
            Assert.Equal("Boil water.", dto.Instructions);
            Assert.Equal("http://image.url", dto.ImageURL);
        }
    }
}
