namespace Backend.Model
{
    public class IngredientUsage
    {
        public int Id { get; set; }
        public int FoodId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }

        public Food Food { get; set; } = null!;
    }

    public class PantryItem : IngredientUsage
    {
        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }

        public class RecipeIngredient : IngredientUsage
    {
        public int RecipeId { get; set; }
        public Recipe Recipe { get; set; } = null!;
    }
}
