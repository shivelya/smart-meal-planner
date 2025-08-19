using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Services
{
    public interface IPantryItemService
    {
        Task<PantryItemDto?> GetPantryItemByIdAsync(int id);
        Task<IEnumerable<PantryItemDto>> GetAllPantryItemsAsync();
        Task<PantryItemDto> CreatePantryItemAsync(PantryItemDto pantryItemDto);
        Task<PantryItemDto> UpdatePantryItemAsync(PantryItemDto pantryItemDto);
        Task<bool> DeletePantryItemAsync(int id);
    }
}
