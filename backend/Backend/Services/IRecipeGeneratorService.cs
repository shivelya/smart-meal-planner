using Backend.DTOs;

namespace Backend.Services
{
    public interface IRecipeGenerator
    {
        Task<CreateUpdateMealPlanRequestDto> GenerateMealPlan(int meals, int userId, bool useExternal);
    }
}