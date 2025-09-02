namespace Backend.DTOs
{
    public class RecipeIngredientDto
    {
        // not used for creation
        public int? Id { get; set; }
        public required FoodReferenceDto Food { get; set; }
        public required decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}