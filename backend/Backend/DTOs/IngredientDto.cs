namespace Backend.DTOs
{
    public class IngredientDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public required CategoryDto Category { get; set; }
    }
}
