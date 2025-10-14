namespace Backend.DTOs
{
    public class MealPlanDto
    {
        public required int Id { get; set; }
        public DateTime? StartDate { get; set; }
        public required IEnumerable<MealPlanEntryDto> Meals { get; set; }
    }

    public class CreateUpdateMealPlanRequestDto
    {
        public int? Id { get; set; }
        public DateTime? StartDate { get; set; }
        public required IEnumerable<CreateUpdateMealPlanEntryRequestDto> Meals { get; set; }
    }

    public class GetMealPlansResult : GetItemsResult<MealPlanDto> { }

    public class GenerateMealPlanRequestDto
    {
        public int Days { get; set; }
        public DateTime StartDate { get; set; }
        public bool UseExternal { get; set; }
    }
}