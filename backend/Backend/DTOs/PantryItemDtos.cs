namespace Backend.DTOs
{
    public class PantryItemDto
    {
        // optional since there won't be one during creates
        public int? Id { get; set; }
        public required FoodReferenceDto Food { get; set; }
        public required decimal Quantity { get; set; }
        public string? Unit { get; set; }
    }

    public class GetPantryItemsResult
    {
        public required int TotalCount { get; set; }
        public required IEnumerable<PantryItemDto> Items { get; set; }
    }

    public class DeleteRequest
    {
        public required IEnumerable<int> Ids { get; set; }
    }

    public class PantrySearchRequest
    {
        public required string Query { get; set; }
    }
}
