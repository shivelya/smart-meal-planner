namespace Backend.DTOs
{
    public class CategoryDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; } = null!;
    }

    public class GetCategoriesResult
    {
        public required int TotalCount { get; set; }
        public required IEnumerable<CategoryDto> Items { get; set; } = [];
    }
}