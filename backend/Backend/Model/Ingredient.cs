namespace Backend.Model
{
    public class Ingredient
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int CategoryId { get; set; }

        public Category Category { get; set; } = null!;
        public ICollection<PantryItem> PantryItems { get; set; } = new List<PantryItem>();
        public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = new List<RecipeIngredient>();
    }
}
