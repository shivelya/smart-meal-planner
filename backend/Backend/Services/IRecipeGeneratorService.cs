using Backend.DTOs;

namespace Backend.Services
{
    public interface IRecipeGenerator
    {
        Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(int meals, int userId, bool useExternal);
    }
}