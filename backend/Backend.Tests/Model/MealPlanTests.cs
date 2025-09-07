using Backend.Model;

namespace Backend.Tests.Model
{
    public class MealPlanTests
    {
        [Fact]
        public void CanCreateMealPlan()
        {
            var mp = new MealPlan();
            Assert.NotNull(mp);
        }

        [Fact]
        public void MealPlan_Id_SetAndGet_Works()
        {
            var mealPlan = new MealPlan();
            mealPlan.Id = 1;
            Assert.Equal(1, mealPlan.Id);
        }

        [Fact]
        public void MealPlan_UserId_SetAndGet_Works()
        {
            var mealPlan = new MealPlan();
            mealPlan.UserId = 2;
            Assert.Equal(2, mealPlan.UserId);
        }

        [Fact]
        public void MealPlan_StartDate_SetAndGet_Works()
        {
            var mealPlan = new MealPlan();
            var date = DateTime.Now;
            mealPlan.StartDate = date;
            Assert.Equal(date, mealPlan.StartDate);
        }

        [Fact]
        public void MealPlan_User_SetAndGet_Works()
        {
            var mealPlan = new MealPlan();
            var user = new User();
            mealPlan.User = user;
            Assert.Same(user, mealPlan.User);
        }

        [Fact]
        public void MealPlan_MealPlanEntries_SetAndGet_Works()
        {
            var mealPlan = new MealPlan();
            var entries = new List<MealPlanEntry>();
            mealPlan.Meals = entries;
            Assert.Same(entries, mealPlan.Meals);
        }
    }
}
