using Backend.DTOs;

namespace Backend.Services
{
    public interface IFoodService
    {
        Task<GetFoodsResult> SearchFoods(string search, int? skip, int? take);
    }
}
