using Backend.DTOs;

namespace Backend.Services
{
    public interface IRecipeIngredientService
    {
        Task<RecipeIngredientDto?> GetRecipeIngredientAsync(int recipeId, int ingredientId);
        Task<IEnumerable<RecipeIngredientDto>> GetAllRecipeIngredientsAsync();
        Task<RecipeIngredientDto> CreateRecipeIngredientAsync(RecipeIngredientDto recipeIngredientDto);
        Task<RecipeIngredientDto> UpdateRecipeIngredientAsync(RecipeIngredientDto recipeIngredientDto);
        Task<bool> DeleteRecipeIngredientAsync(int recipeId, int ingredientId);
    }
}
