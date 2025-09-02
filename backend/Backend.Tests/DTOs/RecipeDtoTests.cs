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
            var dto = new CreateRecipeDtoRequest {
                Id = 5,
                Title = "Soup",
                Source = "Book",
                Instructions = "Heat water.",
                Ingredients = new List<RecipeIngredientDto>()
            };
            Assert.Equal(5, dto.Id);
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
                Ingredients = new List<RecipeIngredientDto>()
            };
            Assert.Equal("Bread", dto.Title);
            Assert.Equal("Web", dto.Source);
            Assert.Equal("Mix flour.", dto.Instructions);
            Assert.Empty(dto.Ingredients);
        }

        private class TestCreateRecipeDto : CreateRecipeDtoRequest { }

        [Fact]
        public void RecipeSearchOptions_PropertyTest()
        {
            var dto = new RecipeSearchOptions {
                TitleContains = "Egg",
                IngredientContains = "Milk",
                Skip = 2,
                Take = 10
            };
            Assert.Equal("Egg", dto.TitleContains);
            Assert.Equal("Milk", dto.IngredientContains);
            Assert.Equal(2, dto.Skip);
            Assert.Equal(10, dto.Take);
        }
    }
}
