namespace Backend.DTOs
{
    public class CategoryDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; } = null!;
    }

    public class GetCategoriesResult : GetItemsResult<CategoryDto> { }
}