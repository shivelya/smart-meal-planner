using Backend.Model;

namespace Backend.Tests.Model
{
    public class CategoryTests
    {
        [Fact]
        public void CanCreateCategory()
        {
            var category = new Category();
            Assert.NotNull(category);
        }

        [Fact]
        public void Category_Id_SetAndGet_Works()
        {
            var category = new Category();
            category.Id = 42;
            Assert.Equal(42, category.Id);
        }

        [Fact]
        public void Category_Name_SetAndGet_Works()
        {
            var category = new Category();
            category.Name = "Vegetables";
            Assert.Equal("Vegetables", category.Name);
        }

        [Fact]
        public void Category_Ingredients_SetAndGet_Works()
        {
            var category = new Category();
            var ingredients = new List<Ingredient>();
            category.Ingredients = ingredients;
            Assert.Same(ingredients, category.Ingredients);
        }
    }
}
