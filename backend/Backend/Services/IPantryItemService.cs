using Backend.DTOs;

namespace Backend.Services
{
    public interface IPantryItemService
    {
        /// <summary>
        /// Retrieves a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>The pantry item DTO if found, otherwise null.</returns>
        Task<PantryItemDto?> GetPantryItemByIdAsync(int id, CancellationToken ct);
        /// <summary>
        /// Retrieves pantry items in a paged manner.
        /// </summary>
        /// <param name="userId">The id of the current user.</param>
        /// <param name="search">The food name to serach for. Optional.</param>
        /// <param name="skip">The number of results to skip for pagination.</param>
        /// <param name="take">The number of results to take for pagination.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>An enumerable collection of pantry item DTOs.</returns>
        Task<GetPantryItemsResult> GetPantryItemsAsync(int userId, string? search, int? take, int? skip, CancellationToken ct);

        /// <summary>
        /// Creates a new pantry item. Assumes that the corresponding Food object has already been created.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to create.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>The created pantry item DTO.</returns>
        Task<PantryItemDto> CreatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId, CancellationToken ct);
        /// <summary>
        /// Creates multiple new pantry items, such as after a grocery trip. Assumes Food objects have already been created.
        /// </summary>
        /// <param name="pantryItemDtos">The list of pantry items to be created.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>The created pantry items.</returns>
        Task<GetPantryItemsResult> CreatePantryItemsAsync(IEnumerable<CreateUpdatePantryItemRequestDto> pantryItemDtos, int userId, CancellationToken ct);

        /// <summary>
        /// Updates an existing pantry item.
        /// </summary>
        /// <param name="pantryItemDto">The pantry item DTO to update.</param>
        /// <param name="userId">The userId the pantry item belongs to.</param>
        /// <param name="id">The id of the pantry item.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>The updated pantry item DTO.</returns>
        Task<PantryItemDto> UpdatePantryItemAsync(CreateUpdatePantryItemRequestDto pantryItemDto, int userId, int id, CancellationToken ct);

        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="userId">The id of the current user.</param>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>True if the pantry item was deleted, otherwise false.</returns>
        Task<bool> DeletePantryItemAsync(int userId, int id, CancellationToken ct);
        /// <summary>
        /// Deletes a list of pantry items, like when they've all been used for a recipe or you're cleaning out your pantry.
        /// </summary>
        /// <param name="userId">The id of the current user.</param>
        /// <param name="ids">The list of pantry item ids to be deleted.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <returns>The number of items that were deleted.</returns>
        Task<DeleteRequest> DeletePantryItemsAsync(int userId, IEnumerable<int> ids, CancellationToken ct);
    }
}
