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

    public class GetMealPlansResult
    {
        public required int TotalCount { get; set; }
        public required IEnumerable<MealPlanDto> MealPlans { get; set;}
    }
}
