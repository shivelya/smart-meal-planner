using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Tests.Model
{
    public class PantryItemTests
    {
        [Fact]
        public void CanCreatePantryItem()
        {
            var item = new PantryItem();
            Assert.NotNull(item);
        }

        [Fact]
        public void PantryItem_Id_SetAndGet_Works()
        {
            var item = new PantryItem();
            item.Id = 7;
            Assert.Equal(7, item.Id);
        }

        [Fact]
        public void PantryItem_UserId_SetAndGet_Works()
        {
            var item = new PantryItem();
            item.UserId = 3;
            Assert.Equal(3, item.UserId);
        }

        [Fact]
        public void PantryItem_IngredientId_SetAndGet_Works()
        {
            var item = new PantryItem();
            item.IngredientId = 8;
            Assert.Equal(8, item.IngredientId);
        }

        [Fact]
        public void PantryItem_Quantity_SetAndGet_Works()
        {
            var item = new PantryItem();
            item.Quantity = 2.5m;
            Assert.Equal(2.5m, item.Quantity);
        }

        [Fact]
        public void PantryItem_Unit_SetAndGet_Works()
        {
            var item = new PantryItem();
            item.Unit = "kg";
            Assert.Equal("kg", item.Unit);
        }

        [Fact]
        public void PantryItem_User_SetAndGet_Works()
        {
            var item = new PantryItem();
            var user = new User();
            item.User = user;
            Assert.Same(user, item.User);
        }

        [Fact]
        public void PantryItem_Ingredient_SetAndGet_Works()
        {
            var item = new PantryItem();
            var ingredient = new Ingredient();
            item.Ingredient = ingredient;
            Assert.Same(ingredient, item.Ingredient);
        }
    }
}
