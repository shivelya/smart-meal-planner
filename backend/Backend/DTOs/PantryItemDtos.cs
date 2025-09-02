namespace Backend.DTOs
{
    public class PantryItemDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public required FoodReferenceDto Food { get; set; }
        public decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class PantryItemRequestDto
    {
        // optional, used for updates but not creates
        public int? Id { get; set; }
        public FoodReferenceDto  Food { get; set; } = null!;
        public required decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class GetPantryItemsResult
    {
        public int TotalCount { get; set; }
        public required IEnumerable<PantryItemDto> Items { get; set; }
    }
}
