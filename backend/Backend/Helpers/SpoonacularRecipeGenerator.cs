using Backend.DTOs;
using Backend.Model;

namespace Backend.Helpers
{
    public interface IRecipeGenerator
    {
        Task<GeneratedMealPlanDto> GenerateMealPlan(int meals, IEnumerable<PantryItem> pantryItems);
    }

    public class SpoonacularRecipeGenerator : IRecipeGenerator
    {
        public Task<GeneratedMealPlanDto> GenerateMealPlan(int meals, IEnumerable<PantryItem> pantryItems)
        {
            throw new NotImplementedException();
        }
    }
}