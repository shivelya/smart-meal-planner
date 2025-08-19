using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Tests.Model
{
    public class RecipeTests
    {
        [Fact]
        public void CanCreateRecipe()
        {
            var recipe = new Recipe();
            Assert.NotNull(recipe);
        }

        [Fact]
        public void Recipe_Id_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.Id = 1;
            Assert.Equal(1, recipe.Id);
        }

        [Fact]
        public void Recipe_UserId_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.UserId = 2;
            Assert.Equal(2, recipe.UserId);
        }

        [Fact]
        public void Recipe_Title_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.Title = "Pasta";
            Assert.Equal("Pasta", recipe.Title);
        }

        [Fact]
        public void Recipe_Source_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.Source = "Cookbook";
            Assert.Equal("Cookbook", recipe.Source);
        }

        [Fact]
        public void Recipe_Instructions_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.Instructions = "Boil water.";
            Assert.Equal("Boil water.", recipe.Instructions);
        }

        [Fact]
        public void Recipe_ImageURL_SetAndGet_Works()
        {
            var recipe = new Recipe();
            recipe.ImageURL = "http://image.url";
            Assert.Equal("http://image.url", recipe.ImageURL);
        }

        [Fact]
        public void Recipe_User_SetAndGet_Works()
        {
            var recipe = new Recipe();
            var user = new User();
            recipe.User = user;
            Assert.Same(user, recipe.User);
        }

        [Fact]
        public void Recipe_RecipeIngredients_SetAndGet_Works()
        {
            var recipe = new Recipe();
            var ingredients = new List<RecipeIngredient>();
            recipe.RecipeIngredients = ingredients;
            Assert.Same(ingredients, recipe.RecipeIngredients);
        }

        [Fact]
        public void Recipe_MealPlanEntries_SetAndGet_Works()
        {
            var recipe = new Recipe();
            var entries = new List<MealPlanEntry>();
            recipe.MealPlanEntries = entries;
            Assert.Same(entries, recipe.MealPlanEntries);
        }
    }
}
