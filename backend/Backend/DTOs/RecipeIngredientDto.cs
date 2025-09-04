namespace Backend.DTOs
{
    public class RecipeIngredientDto
    {
        public int Id { get; set; }
        public required FoodDto Food { get; set; }
        public required decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class CreateUpdateRecipeIngredientDto
    {
        // not used for creation
        public int? Id { get; set; }
        public required FoodReferenceDto Food { get; set; }
        public required decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }
}