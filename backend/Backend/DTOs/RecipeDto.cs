namespace SmartMealPlannerBackend.DTOs
{
    public class RecipeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public string? ImageURL { get; set; }
    }
}
