namespace Backend.DTOs
{
    public class GenerateShoppingListRequestDto
    {
        public bool Restart { get; set; }
        public required int MealPlanId { get; set; }
    }

    public class GetShoppingListResult : GetItemsResult<ShoppingListItemDto> { }

    public class ShoppingListItemDto
    {
        public required int Id { get; set; }
        public int? FoodId { get; set; }
        public FoodDto? Food { get; set; }
        public bool Purchased { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateUpdateShoppingListEntryRequestDto
    {
        public int? Id { get; set; } // null for create, required for update
        public int? FoodId { get; set; }
        public bool Purchased { get; set; }
        public string? Notes { get; set; }
    }
}