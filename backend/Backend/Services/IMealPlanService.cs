using Backend.DTOs;

namespace Backend.Services
{
    public interface IMealPlanService
    {
        /// <summary>
        /// Retrieves all meal plans.
        /// </summary>
        /// <param name="userId">Id of the current user.</param>
        /// <param name="skip">The number to skip for pagination.</param>
        /// <param name="take">The number to take for pagination.</param>
        /// <returns>An enumerable collection of meal plan DTOs along with the total count.</returns>
        Task<GetMealPlansResult> GetMealPlansAsync(int userId, int? skip, int? take);
        /// <summary>
        /// Creates a new meal plan.
        /// </summary>
        /// <param name="userId">The id of the logged in user.</param>
        /// <param name="request">The meal plan DTO to create.</param>
        /// <returns>The created meal plan DTO.</returns>
        Task<MealPlanDto> AddMealPlanAsync(int userId, CreateUpdateMealPlanRequestDto request);
        /// <summary>
        /// Updates an existing meal plan.
        /// </summary>
        /// <param name="id">The id of the meal plan to update.</param>
        /// <param name="userId">The id of the logged in user.</param>
        /// <param name="request">The meal plan DTO to update.</param>
        /// <returns>The updated meal plan DTO.</returns>
        Task<MealPlanDto> UpdateMealPlanAsync(int id, int userId, CreateUpdateMealPlanRequestDto request);
        /// <summary>
        /// Deletes a meal plan by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan's unique identifier.</param>
        /// <param name="userId">The id of the logged in user.</param>
        /// <returns>True if the meal plan was deleted, otherwise false.</returns>
        Task<bool> DeleteMealPlanAsync(int id, int userId);
        /// <summary>
        /// Generates a meal plan based on the users pantry items.
        /// </summary>
        /// <param name="request">The request to create a meal plan including number of recipes to add, the start date, and whether to use external recipes.</param>
        /// <param name="userId">The id of the current user.</param>
        /// <returns>The generated meal plan.</returns>
        Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(GenerateMealPlanRequestDto request, int userId);
        /// <summary>
        /// Gets a list of pantry items that may have been used when the given meal was cooked.
        /// </summary>
        /// <param name="id">The id of the meal plan.</param>
        /// <param name="mealEntryId">The id of the meal within the meal plan being cooked.</param>
        /// <param name="userId">The id of the current user.</param>
        /// <returns>A list of pantry items used in cooking the meal.</returns>
        Task<GetPantryItemsResult> CookMealAsync(int id, int mealEntryId, int userId);
    }
}
