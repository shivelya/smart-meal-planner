using Backend.DTOs;

namespace Backend.Services
{
    public interface IPantryItemService
    {
        /// <summary>
        /// Retrieves a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <returns>The pantry item DTO if found, otherwise null.</returns>
        Task<PantryItemDto?> GetPantryItemByIdAsync(int id);
        /// <summary>
        /// Retrieves all pantry items.
        /// </summary>
        /// <returns>An enumerable collection of pantry item DTOs.</returns>
        Task<IEnumerable<PantryItemDto>> GetAllPantryItemsAsync();
        /// <summary>
        /// Creates a new pantry item.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to create.</param>
        /// <returns>The created pantry item DTO.</returns>
        Task<PantryItemDto> CreatePantryItemAsync(PantryItemDto pantryItemDto);
        /// <summary>
        /// Updates an existing pantry item.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to update.</param>
        /// <returns>The updated pantry item DTO.</returns>
        Task<PantryItemDto> UpdatePantryItemAsync(PantryItemDto pantryItemDto);
        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <returns>True if the pantry item was deleted, otherwise false.</returns>
        Task<bool> DeletePantryItemAsync(int id);
    }
}
