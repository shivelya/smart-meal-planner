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
        //required for update but not for create
        public int? Id { get; set; }
        public required string Source { get; set; }
        public required string Title { get; set; }
        public required string Instructions { get; set; }
        // if provided, replaces all existing ingredients
        public required List<CreateUpdateRecipeIngredientDto> Ingredients { get; set; } = [];
    }

    public class GetRecipesResult
    {
        public required int TotalCount { get; set; }
        public required IEnumerable<RecipeDto> Items { get; set; }
    }

    public class GetRecipesRequest
    {
        public required IEnumerable<int> Ids { get; set; }
    }

    public class ExtractRequest
    {
        public required string Source { get; set; }
    }
}