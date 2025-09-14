using Backend.DTOs;

namespace Backend.Services
{
    public interface IShoppingListService
    {
        Task GenerateAsync(GenerateShoppingListRequestDto request, int userId);
        Task<GetShoppingListResult> GetShoppingListAsync(int userId);
        Task<ShoppingListItemDto> UpdateShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId);
        Task<ShoppingListItemDto> AddShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId);
        Task<bool> DeleteShoppingListItemAsync(int id, int userId);
    }
}