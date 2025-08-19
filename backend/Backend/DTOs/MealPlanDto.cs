namespace Backend.DTOs
{
    public class MealPlanDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime? StartDate { get; set; }
    }
}
