namespace Backend.Model
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;

        public ICollection<PantryItem> PantryItems { get; set; } = new List<PantryItem>();
        public ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public ICollection<MealPlan> MealPlans { get; set; } = new List<MealPlan>();
    }
}
