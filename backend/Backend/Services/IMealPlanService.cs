using Backend.DTOs;

namespace Backend.Services
{
    public interface IMealPlanService
    {
        /// <summary>
        /// Retrieves a meal plan by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan's unique identifier.</param>
        /// <returns>The meal plan DTO if found, otherwise null.</returns>
        Task<MealPlanDto?> GetMealPlanByIdAsync(int id);
        /// <summary>
        /// Retrieves all meal plans.
        /// </summary>
        /// <returns>An enumerable collection of meal plan DTOs.</returns>
        Task<IEnumerable<MealPlanDto>> GetAllMealPlansAsync();
        /// <summary>
        /// Creates a new meal plan.
        /// </summary>
        /// <param name="mealPlanDto">The meal plan DTO to create.</param>
        /// <returns>The created meal plan DTO.</returns>
        Task<MealPlanDto> CreateMealPlanAsync(MealPlanDto mealPlanDto);
        /// <summary>
        /// Updates an existing meal plan.
        /// </summary>
        /// <param name="mealPlanDto">The meal plan DTO to update.</param>
        /// <returns>The updated meal plan DTO.</returns>
        Task<MealPlanDto> UpdateMealPlanAsync(MealPlanDto mealPlanDto);
        /// <summary>
        /// Deletes a meal plan by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan's unique identifier.</param>
        /// <returns>True if the meal plan was deleted, otherwise false.</returns>
        Task<bool> DeleteMealPlanAsync(int id);
    }
}
