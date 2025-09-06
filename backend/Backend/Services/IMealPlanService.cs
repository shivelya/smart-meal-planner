using Backend.DTOs;

namespace Backend.Services
{
    public interface IMealPlanService
    {
        /// <summary>
        /// Retrieves all meal plans.
        /// </summary>
        /// <param name="skip">The number to skip for pagination.</param>
        /// <param name="take">The number to take for pagination.</param>
        /// <returns>An enumerable collection of meal plan DTOs along with the total count.</returns>
        Task<GetMealPlansResult> GetMealPlansAsync(int? skip, int? take);
        /// <summary>
        /// Creates a new meal plan.
        /// </summary>
        /// <param name="request">The meal plan DTO to create.</param>
        /// <returns>The created meal plan DTO.</returns>
        Task<MealPlanDto> AddMealPlanAsync(CreateUpdateMealPlanRequestDto request);
        /// <summary>
        /// Updates an existing meal plan.
        /// </summary>
        /// <param name="id">The id of the meal plan to update.</param>
        /// <param name="request">The meal plan DTO to update.</param>
        /// <returns>The updated meal plan DTO.</returns>
        Task<MealPlanDto> UpdateMealPlanAsync(int id, CreateUpdateMealPlanRequestDto request);
        /// <summary>
        /// Deletes a meal plan by its unique ID.
        /// </summary>
        /// <param name="id">The meal plan's unique identifier.</param>
        /// <returns>True if the meal plan was deleted, otherwise false.</returns>
        Task<bool> DeleteMealPlanAsync(int id);
        /// <summary>
        /// Generates a meal plan based on the users pantry items.
        /// </summary>
        /// <param name="days">The of recipes to add.</param>
        /// <param name="startDate">The start date for the meal plan.</param>
        /// <returns>The generated meal plan.</returns>
        Task<GeneratedMealPlanDto> GenerateMealPlanAsync(int days, DateTime startDate);
    }
}
