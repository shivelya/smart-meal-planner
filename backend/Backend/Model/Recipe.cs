namespace Backend.Model
{
    public class Recipe
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public string? ImageURL { get; set; }

        public User User { get; set; } = null!;
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
        public ICollection<MealPlanEntry> MealPlanEntries { get; set; } = new List<MealPlanEntry>();
    }
}
