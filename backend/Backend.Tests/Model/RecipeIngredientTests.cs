using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Tests.Model
{
    public class RecipeIngredientTests
    {
        [Fact]
        public void CanCreateRecipeIngredient()
        {
            var ri = new RecipeIngredient();
            Assert.NotNull(ri);
        }

        [Fact]
        public void RecipeIngredient_RecipeId_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            ri.RecipeId = 11;
            Assert.Equal(11, ri.RecipeId);
        }

        [Fact]
        public void RecipeIngredient_IngredientId_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            ri.IngredientId = 22;
            Assert.Equal(22, ri.IngredientId);
        }

        [Fact]
        public void RecipeIngredient_Quantity_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            ri.Quantity = 1.75m;
            Assert.Equal(1.75m, ri.Quantity);
        }

        [Fact]
        public void RecipeIngredient_Unit_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            ri.Unit = "cup";
            Assert.Equal("cup", ri.Unit);
        }

        [Fact]
        public void RecipeIngredient_Recipe_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            var recipe = new Recipe();
            ri.Recipe = recipe;
            Assert.Same(recipe, ri.Recipe);
        }

        [Fact]
        public void RecipeIngredient_Ingredient_SetAndGet_Works()
        {
            var ri = new RecipeIngredient();
            var ingredient = new Ingredient();
            ri.Ingredient = ingredient;
            Assert.Same(ingredient, ri.Ingredient);
        }
    }
}
