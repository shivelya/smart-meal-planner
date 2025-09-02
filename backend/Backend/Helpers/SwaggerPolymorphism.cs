using Backend.DTOs;

namespace Backend.Helpers
{
    public class ExistingIngredientDto : FoodReferenceDto
    {
        public new string Mode { get; set; } = "existing";
        public new int Id { get; set; }
    }

    public class NewIngredientDto : FoodReferenceDto
    {
        public new string Mode { get; set; } = "new";
        public new string Name { get; set; } = null!;
        public new int CategoryId { get; set; }
    }
}