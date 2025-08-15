namespace SmartMealPlannerBackend.Model
{
    public class MealPlanEntry
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }
        public int RecipeId { get; set; }
        public DateTime? Date { get; set; }

        public MealPlan MealPlan { get; set; } = null!;
        public Recipe Recipe { get; set; } = null!;
    }
}
