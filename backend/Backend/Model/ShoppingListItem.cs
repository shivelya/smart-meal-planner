namespace Backend.Model
{
    public class ShoppingListItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? FoodId { get; set; }
        public string? Notes { get; set; }
        public bool Purchased { get; set; }

        public User User { get; set; } = null!;
        public Food? Food { get; set; }
    }
}