using Backend.DTOs;

namespace Backend.Services
{
    public interface IIngredientService
    {
        Task<IEnumerable<IngredientDto>> SearchIngredients(string search);
    }
}
