using Backend.DTOs;

namespace Backend.Services
{
    public interface IShoppingListService
    {
        Task GenerateAsync(GenerateShoppingListRequestDto request, int userId);
    }
}