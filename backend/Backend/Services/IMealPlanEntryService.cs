using SmartMealPlannerBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartMealPlannerBackend.Services
{
    public interface IMealPlanEntryService
    {
        Task<MealPlanEntryDto?> GetMealPlanEntryByIdAsync(int id);
        Task<IEnumerable<MealPlanEntryDto>> GetAllMealPlanEntriesAsync();
        Task<MealPlanEntryDto> CreateMealPlanEntryAsync(MealPlanEntryDto mealPlanEntryDto);
        Task<MealPlanEntryDto> UpdateMealPlanEntryAsync(MealPlanEntryDto mealPlanEntryDto);
        Task<bool> DeleteMealPlanEntryAsync(int id);
    }
}
