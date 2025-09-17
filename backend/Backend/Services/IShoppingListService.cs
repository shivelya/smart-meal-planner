using Backend.DTOs;

namespace Backend.Services
{
    public interface IShoppingListService
    {
        /// <summary>
        /// Generates a shopping list for the specified user and meal plan.
        /// </summary>
        /// <param name="request">The request containing meal plan id and restart flag.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        Task GenerateAsync(GenerateShoppingListRequestDto request, int userId, CancellationToken ct = default);

        /// <summary>
        /// Retrieves the shopping list for the specified user.
        /// </summary>
        /// <param name="userId">The id of the user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>The shopping list result.</returns>
        Task<GetShoppingListResult> GetShoppingListAsync(int userId, CancellationToken ct = default);

        /// <summary>
        /// Updates an item on the shopping list for the specified user.
        /// </summary>
        /// <param name="request">The updated shopping list item.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>The updated shopping list item.</returns>
        Task<ShoppingListItemDto> UpdateShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId, CancellationToken ct = default);

        /// <summary>
        /// Adds a new item to the shopping list for the specified user.
        /// </summary>
        /// <param name="request">The new shopping list item.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>The added shopping list item.</returns>
        Task<ShoppingListItemDto> AddShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, int userId, CancellationToken ct = default);

        /// <summary>
        /// Deletes an item from the shopping list for the specified user.
        /// </summary>
        /// <param name="id">The id of the shopping list item to delete.</param>
        /// <param name="userId">The id of the user.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>True if the item was deleted, otherwise false.</returns>
        Task<bool> DeleteShoppingListItemAsync(int id, int userId, CancellationToken ct = default);
    }
}