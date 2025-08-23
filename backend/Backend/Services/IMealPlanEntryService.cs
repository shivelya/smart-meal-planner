using Backend.DTOs;

namespace Backend.Services
{
    public interface IMealPlanEntryService
    {
        /// <summary>
        /// Retrieves a meal plan entry by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan entry's unique identifier.</param>
        /// <returns>The meal plan entry DTO if found, otherwise null.</returns>
        Task<MealPlanEntryDto?> GetMealPlanEntryByIdAsync(int id);
        /// <summary>
        /// Retrieves all meal plan entries.
        /// </summary>
        /// <returns>An enumerable collection of meal plan entry DTOs.</returns>
        Task<IEnumerable<MealPlanEntryDto>> GetAllMealPlanEntriesAsync();
        /// <summary>
        /// Creates a new meal plan entry.
        /// </summary>
        /// <param name="mealPlanEntryDto">The meal plan entry DTO to create.</param>
        /// <returns>The created meal plan entry DTO.</returns>
        Task<MealPlanEntryDto> CreateMealPlanEntryAsync(MealPlanEntryDto mealPlanEntryDto);
        /// <summary>
        /// Updates an existing meal plan entry.
        /// </summary>
        /// <param name="mealPlanEntryDto">The meal plan entry DTO to update.</param>
        /// <returns>The updated meal plan entry DTO.</returns>
        Task<MealPlanEntryDto> UpdateMealPlanEntryAsync(MealPlanEntryDto mealPlanEntryDto);
        /// <summary>
        /// Deletes a meal plan entry by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan entry's unique identifier.</param>
        /// <returns>True if the meal plan entry was deleted, otherwise false.</returns>
        Task<bool> DeleteMealPlanEntryAsync(int id);
    }
}
