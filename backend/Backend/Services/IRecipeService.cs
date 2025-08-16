using SmartMealPlannerBackend.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SmartMealPlannerBackend.Services
{
    public interface IRecipeService
    {
        Task<RecipeDto?> GetRecipeByIdAsync(int id);
        Task<IEnumerable<RecipeDto>> GetAllRecipesAsync();
        Task<RecipeDto> CreateRecipeAsync(RecipeDto recipeDto);
        Task<RecipeDto> UpdateRecipeAsync(RecipeDto recipeDto);
        Task<bool> DeleteRecipeAsync(int id);
    }
}
