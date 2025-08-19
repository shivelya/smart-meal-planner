namespace SmartMealPlannerBackend.DTOs
{
    public class MealPlanEntryDto
    {
        public int Id { get; set; }
        public int MealPlanId { get; set; }
        public int RecipeId { get; set; }
        public DateTime? Date { get; set; }
    }
}
