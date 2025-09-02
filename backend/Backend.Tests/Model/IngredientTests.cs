using Backend.Model;

namespace Backend.Tests.Model
{
    public class IngredientTests
    {
        [Fact]
        public void CanCreateIngredient()
        {
            var ingredient = new Food();
            Assert.NotNull(ingredient);
        }

        [Fact]
        public void Ingredient_Id_SetAndGet_Works()
        {
            var ingredient = new Food();
            ingredient.Id = 101;
            Assert.Equal(101, ingredient.Id);
        }

        [Fact]
        public void Ingredient_Name_SetAndGet_Works()
        {
            var ingredient = new Food();
            ingredient.Name = "Tomato";
            Assert.Equal("Tomato", ingredient.Name);
        }

        [Fact]
        public void Ingredient_CategoryId_SetAndGet_Works()
        {
            var ingredient = new Food();
            ingredient.CategoryId = 5;
            Assert.Equal(5, ingredient.CategoryId);
        }

        [Fact]
        public void Ingredient_Category_SetAndGet_Works()
        {
            var ingredient = new Food();
            var category = new Category();
            ingredient.Category = category;
            Assert.Same(category, ingredient.Category);
        }
    }
}
