using Backend.DTOs;

namespace Backend.Services
{
    public interface IMealPlanGenerator
    {
        Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(int meals, int userId, bool useExternal);
    }
}