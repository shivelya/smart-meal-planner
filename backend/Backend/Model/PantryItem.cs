namespace Backend.Model
{
    public class PantryItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }

        public User User { get; set; } = null!;
        public Ingredient Ingredient { get; set; } = null!;
    }
}
