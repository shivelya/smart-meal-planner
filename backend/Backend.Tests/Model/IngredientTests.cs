using Backend.Model;

namespace Backend.Tests.Model
{
    public class FoodTests
    {
        [Fact]
        public void CanCreateFood()
        {
            var food = new Food();
            Assert.NotNull(food);
        }

        [Fact]
        public void Food_Id_SetAndGet_Works()
        {
            var food = new Food();
            food.Id = 101;
            Assert.Equal(101, food.Id);
        }

        [Fact]
        public void Food_Name_SetAndGet_Works()
        {
            var food = new Food();
            food.Name = "Tomato";
            Assert.Equal("Tomato", food.Name);
        }

        [Fact]
        public void Food_CategoryId_SetAndGet_Works()
        {
            var food = new Food();
            food.CategoryId = 5;
            Assert.Equal(5, food.CategoryId);
        }

        [Fact]
        public void Food_Category_SetAndGet_Works()
        {
            var food = new Food();
            var category = new Category();
            food.Category = category;
            Assert.Same(category, food.Category);
        }
    }
}
