using Backend.DTOs;

namespace Backend.Services
{
    public interface IFoodService
    {
        Task<IEnumerable<FoodDto>> SearchFoods(string search);
    }
}
