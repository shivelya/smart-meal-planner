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

    public abstract class CreatePantryItemDto
    {
        // optional, used for updates but not creates
        public int? Id { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class CreatePantryItemNewIngredientDto : CreatePantryItemDto
    {
        public required string IngredientName { get; set; }
        public int CategoryId { get; set; }
    }

    public class CreatePantryItemOldIngredientDto : CreatePantryItemDto
    {
        public int IngredientId { get; set; }
    }

    public class GetPantryItemsResult
    {
        public int TotalCount { get; set; }
        public required IEnumerable<PantryItemDto> Items { get; set; }
    }
}
