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
        /// Retrieves all pantry items in a paged manner.
        /// </summary>
        /// <returns>An enumerable collection of pantry item DTOs.</returns>
        Task<(IEnumerable<PantryItemDto> Items, int TotalCount)> GetAllPantryItemsAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Creates a new pantry item. Assumes that the corresponding Ingredient object has already been created.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to create.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>The created pantry item DTO.</returns>
        Task<PantryItemDto> CreatePantryItemAsync(CreatePantryItemDto pantryItemDto, int userId);
        /// <summary>
        /// Creates multiple new pantry items, such as after a grocery trip. Assumes ingredient objects have already been created.
        /// </summary>
        /// <param name="pantryItemDtos">The list of pantry items to be created.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>The created pantry items.</returns>
        Task<IEnumerable<PantryItemDto>> CreatePantryItemsAsync(IEnumerable<CreatePantryItemDto> pantryItemDtos, int userId);

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
        /// <summary>
        /// Deletes a list of pantry items, like when they've all been used for a recipe or you're cleaning out your pantry.
        /// </summary>
        /// <param name="ids">The list of pantry item ids to be deleted.</param>
        /// <returns>The number of items that were deleted.</returns>
        Task<int> DeletePantryItemsAsync(IEnumerable<int> ids);
    }
}
