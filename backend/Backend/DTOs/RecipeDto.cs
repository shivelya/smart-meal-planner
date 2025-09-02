namespace Backend.DTOs
{
    public class RecipeDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; } = null!;
        public string? Source { get; set; } = null!;
        public string Instructions { get; set; } = null!;
        public List<RecipeIngredientDto> Ingredients { get; set; } = [];
    }

    public class CreateRecipeDtoRequest
    { 
        //required for update but not for create
        public int? Id { get; set; }
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Instructions { get; set; }
        // if provided, replaces all existing ingredients
        public required List<RecipeIngredientDto> Ingredients { get; set; } = [];
    }

    public class RecipeSearchOptions
    {
        public string? TitleContains { get; set; } = null!;
        public string? IngredientContains { get; set; } = null!;
        public int? Skip = 0;
        public int? Take = 50;
    }
}