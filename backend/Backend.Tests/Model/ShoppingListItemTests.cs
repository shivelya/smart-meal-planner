using Backend.Model;

namespace Backend.Tests.Model
{
    public class ShoppingListItemTests
    {
        [Fact]
        public void CanSetAndGetProperties()
        {
            var user = new User { Id = 1, Email = "test@example.com" };
            var food = new Food { Id = 2, Name = "Banana" };
            var item = new ShoppingListItem
            {
                Id = 10,
                UserId = 1,
                FoodId = 2,
                Notes = "Ripe",
                Purchased = true,
                User = user,
                Food = food
            };
            Assert.Equal(10, item.Id);
            Assert.Equal(1, item.UserId);
            Assert.Equal(2, item.FoodId);
            Assert.Equal("Ripe", item.Notes);
            Assert.True(item.Purchased);
            Assert.Equal(user, item.User);
            Assert.Equal(food, item.Food);
        }

        [Fact]
        public void CanSetNullProperties()
        {
            var user = new User { Id = 1, Email = "test@example.com" };
            var item = new ShoppingListItem
            {
                Id = 11,
                UserId = 1,
                FoodId = null,
                Notes = null,
                Purchased = false,
                User = user,
                Food = null
            };
            Assert.Null(item.FoodId);
            Assert.Null(item.Notes);
            Assert.Null(item.Food);
        }
    }
}
