using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Tests.Model
{
    public class IngredientTests
    {
        [Fact]
        public void CanCreateIngredient()
        {
            var ingredient = new Ingredient();
            Assert.NotNull(ingredient);
        }

        [Fact]
        public void Ingredient_Id_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            ingredient.Id = 101;
            Assert.Equal(101, ingredient.Id);
        }

        [Fact]
        public void Ingredient_Name_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            ingredient.Name = "Tomato";
            Assert.Equal("Tomato", ingredient.Name);
        }

        [Fact]
        public void Ingredient_CategoryId_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            ingredient.CategoryId = 5;
            Assert.Equal(5, ingredient.CategoryId);
        }

        [Fact]
        public void Ingredient_Category_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            var category = new Category();
            ingredient.Category = category;
            Assert.Same(category, ingredient.Category);
        }

        [Fact]
        public void Ingredient_PantryItems_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            var pantryItems = new List<PantryItem>();
            ingredient.PantryItems = pantryItems;
            Assert.Same(pantryItems, ingredient.PantryItems);
        }

        [Fact]
        public void Ingredient_RecipeIngredients_SetAndGet_Works()
        {
            var ingredient = new Ingredient();
            var recipeIngredients = new List<RecipeIngredient>();
            ingredient.RecipeIngredients = recipeIngredients;
            Assert.Same(recipeIngredients, ingredient.RecipeIngredients);
        }
    }
}
