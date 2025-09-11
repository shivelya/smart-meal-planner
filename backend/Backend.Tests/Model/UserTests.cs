using Backend.Model;

namespace Backend.Tests.Model
{
        public class UserTests
        {
            [Fact]
            public void User_ShoppingList_SetAndGet_Works()
            {
                var user = new User();
                Assert.NotNull(user.ShoppingList);
                Assert.Empty(user.ShoppingList);

                var item = new ShoppingListItem { Id = 2, UserId = 1 };
                user.ShoppingList.Add(item);
                Assert.Single(user.ShoppingList);
                Assert.Equal(item, user.ShoppingList.First());

                var newList = new List<ShoppingListItem> { new ShoppingListItem { Id = 3, UserId = 1 } };
                user.ShoppingList = newList;
                Assert.Equal(newList, user.ShoppingList);
            }

            [Fact]
            public void CanCreateUser()
            {
                var user = new User();
                Assert.NotNull(user);
            }

            [Fact]
            public void User_Id_SetAndGet_Works()
            {
                var user = new User();
                user.Id = 1;
                Assert.Equal(1, user.Id);
            }

            [Fact]
            public void User_Email_SetAndGet_Works()
            {
                var user = new User();
                user.Email = "test@example.com";
                Assert.Equal("test@example.com", user.Email);
            }

            [Fact]
            public void User_PasswordHash_SetAndGet_Works()
            {
                var user = new User();
                user.PasswordHash = "hash";
                Assert.Equal("hash", user.PasswordHash);
            }

            [Fact]
            public void User_PantryItems_SetAndGet_Works()
            {
                var user = new User();
                var pantryItems = new List<PantryItem>();
                user.PantryItems = pantryItems;
                Assert.Same(pantryItems, user.PantryItems);
            }

            [Fact]
            public void User_Recipes_SetAndGet_Works()
            {
                var user = new User();
                var recipes = new List<Recipe>();
                user.Recipes = recipes;
                Assert.Same(recipes, user.Recipes);
            }

            [Fact]
            public void User_MealPlans_SetAndGet_Works()
            {
                var user = new User();
                var mealPlans = new List<MealPlan>();
                user.MealPlans = mealPlans;
                Assert.Same(mealPlans, user.MealPlans);
            }
        }
}
