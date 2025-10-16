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

    public class CreateUpdateRecipeDtoRequest
    {
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Instructions { get; set; }
        // if provided, replaces all existing ingredients
        public required List<CreateUpdateRecipeIngredientDto> Ingredients { get; set; } = [];
    }

    public class GetRecipesResult : GetItemsResult<RecipeDto> { }

    public class GetRecipesRequest
    {
        public required IEnumerable<int> Ids { get; set; }
    }

    public class ExtractRequest
    {
        public required string Source { get; set; }
    }
}