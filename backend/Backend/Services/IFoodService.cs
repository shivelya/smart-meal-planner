using Backend.DTOs;

namespace Backend.Services
{
    public interface IFoodService
    {
        Task<GetFoodsResult> SearchFoodsAsync(string search, int? skip, int? take);
    }
}
