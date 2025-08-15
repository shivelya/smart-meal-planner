namespace SmartMealPlannerBackend.Model
{
    public class MealPlan
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime? StartDate { get; set; }

        public User User { get; set; } = null!;
        public ICollection<MealPlanEntry> MealPlanEntries { get; set; } = new List<MealPlanEntry>();
    }
}
