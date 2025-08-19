namespace SmartMealPlannerBackend.DTOs
{
    public class PantryItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int IngredientId { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}
