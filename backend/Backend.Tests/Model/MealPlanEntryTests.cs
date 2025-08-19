using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Tests.Model
{
    public class MealPlanEntryTests
    {
        [Fact]
        public void CanCreateMealPlanEntry()
        {
            var mpe = new MealPlanEntry();
            Assert.NotNull(mpe);
        }

        [Fact]
        public void MealPlanEntry_Id_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            entry.Id = 1;
            Assert.Equal(1, entry.Id);
        }

        [Fact]
        public void MealPlanEntry_MealPlanId_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            entry.MealPlanId = 2;
            Assert.Equal(2, entry.MealPlanId);
        }

        [Fact]
        public void MealPlanEntry_RecipeId_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            entry.RecipeId = 3;
            Assert.Equal(3, entry.RecipeId);
        }

        [Fact]
        public void MealPlanEntry_Date_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            var date = DateTime.Now;
            entry.Date = date;
            Assert.Equal(date, entry.Date);
        }

        [Fact]
        public void MealPlanEntry_MealPlan_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            var mealPlan = new MealPlan();
            entry.MealPlan = mealPlan;
            Assert.Same(mealPlan, entry.MealPlan);
        }

        [Fact]
        public void MealPlanEntry_Recipe_SetAndGet_Works()
        {
            var entry = new MealPlanEntry();
            var recipe = new Recipe();
            entry.Recipe = recipe;
            Assert.Same(recipe, entry.Recipe);
        }
    }
}
