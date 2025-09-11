using Backend.DTOs;

namespace Backend.Services
{
    public interface IShoppingListService
    {
        Task GenerateAsync(GenerateShoppingListRequestDto request, int userId);
        GetShoppingListResult GetShoppingList(int userId);
        Task<ShoppingListItemDto> UpdateShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId);
    }
}