using SmartMealPlannerBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartMealPlannerBackend.Services
{
    public interface IMealPlanService
    {
        Task<MealPlanDto?> GetMealPlanByIdAsync(int id);
        Task<IEnumerable<MealPlanDto>> GetAllMealPlansAsync();
        Task<MealPlanDto> CreateMealPlanAsync(MealPlanDto mealPlanDto);
        Task<MealPlanDto> UpdateMealPlanAsync(MealPlanDto mealPlanDto);
        Task<bool> DeleteMealPlanAsync(int id);
    }
}
