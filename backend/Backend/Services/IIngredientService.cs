using Backend.DTOs;

namespace Backend.Services
{
    public interface IFoodService
    {
        Task<IEnumerable<FoodReferenceDto>> SearchFoods(string search);
    }
}
