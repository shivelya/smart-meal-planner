using Backend.DTOs;

namespace Backend.Services
{
    public interface IIngredientService
    {
        Task<IEnumerable<FoodReferenceDto>> SearchIngredients(string search);
    }
}
