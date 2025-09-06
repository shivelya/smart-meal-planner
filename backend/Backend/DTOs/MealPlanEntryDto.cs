namespace Backend.DTOs
{
    public class MealPlanEntryDto
    {
        public required int Id { get; set; }
        public string? Notes { get; set; }
        public int? RecipeId { get; set; }
        public RecipeDto? Recipe { get; set; }
    }

    public class GeneratedMealPlanEntryDto
    {
        public int RecipeId { get; set; }
        public required RecipeDto Recipe { get; set; }
    }

    public class CreateUpdateMealPlanEntryRequestDto
    {
        public int? Id { get; set; }
        public string? Notes { get; set; }
        public int? RecipeId { get; set; }
    }
}
