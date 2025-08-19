namespace SmartMealPlannerBackend.Model
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        public ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
    }
}
