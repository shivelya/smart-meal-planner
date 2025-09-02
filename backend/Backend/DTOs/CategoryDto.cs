namespace Backend.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class GetCategoriesResult
    {
        public int TotalCount { get; set; }
        public IEnumerable<CategoryDto> Items { get; set; } = [];
    }
}