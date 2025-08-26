namespace Backend.DTOs
{
    public class RecipeIngredientDto
    {
        public required IngredientDto Ingredient { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public abstract class CreateRecipeIngredientDto
    {
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class CreateRecipeIngredientOldIngredientDto : CreateRecipeIngredientDto
    {
        public int IngredientId { get; set; }
    }

    public class CreateRecipeIngredientNewIngredientDto : CreateRecipeIngredientDto
    {
        public required string IngredientName { get; set; }
        public int CategoryId { get; set; }
    }
}
