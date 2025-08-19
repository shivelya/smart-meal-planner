using Backend.DTOs;

namespace Backend.Services
{
    public interface IIngredientService
    {
        Task<IngredientDto?> GetIngredientByIdAsync(int id);
        Task<IEnumerable<IngredientDto>> GetAllIngredientsAsync();
        Task<IngredientDto> CreateIngredientAsync(IngredientDto ingredientDto);
        Task<IngredientDto> UpdateIngredientAsync(IngredientDto ingredientDto);
        Task<bool> DeleteIngredientAsync(int id);
    }
}
