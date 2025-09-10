namespace Backend.DTOs
{
    public class MealPlanEntryDto
    {
        public required int Id { get; set; }
        public string? Notes { get; set; }
        public int? RecipeId { get; set; }
        public bool Cooked { get; set; }
        public RecipeDto? Recipe { get; set; }
    }

    public class GeneratedMealPlanEntryDto : CreateUpdateMealPlanEntryRequestDto
    {
        public required string Title { get; set; }
        public required string Source { get; set; }
        public required string Instructions { get; set; }
    }

    public class CreateUpdateMealPlanEntryRequestDto
    {
        public int? Id { get; set; }
        public string? Notes { get; set; }
        public int? RecipeId { get; set; }
    }
}
