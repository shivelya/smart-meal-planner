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
                Instructions = "Boil water."
            };
            Assert.Equal(4, dto.Id);
            Assert.Equal(1, dto.UserId);
            Assert.Equal("Pasta", dto.Title);
            Assert.Equal("Cookbook", dto.Source);
            Assert.Equal("Boil water.", dto.Instructions);
        }

        [Fact]
        public void UpdateRecipeDto_PropertyTest()
        {
            var dto = new CreateUpdateRecipeDtoRequest {
                Title = "Soup",
                Source = "Book",
                Instructions = "Heat water.",
                Ingredients = []
            };
            Assert.Equal("Soup", dto.Title);
            Assert.Equal("Book", dto.Source);
            Assert.Equal("Heat water.", dto.Instructions);
            Assert.Empty(dto.Ingredients);
        }

        [Fact]
        public void CreateRecipeDto_PropertyTest()
        {
            var dto = new TestCreateRecipeDto {
                Title = "Bread",
                Source = "Web",
                Instructions = "Mix flour.",
                Ingredients = []
            };
            Assert.Equal("Bread", dto.Title);
            Assert.Equal("Web", dto.Source);
            Assert.Equal("Mix flour.", dto.Instructions);
            Assert.Empty(dto.Ingredients);
        }

        private class TestCreateRecipeDto : CreateUpdateRecipeDtoRequest { }
    }
}
