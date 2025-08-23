namespace Backend.DTOs
{
    public class PantryItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class CreatePantryItemDto
    {
        // behind the scenes when a new pantry item is created we'll connect it to an existing ingredient or add a new one
        public int? IngredientId { get; set; }
        public string? IngredientName { get; set; }
        public int? CategoryId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}
