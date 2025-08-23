using Backend.DTOs;

namespace Backend.Services
{
    public interface IIngredientService
    {
        /// <summary>
        /// Retrieves an ingredient by its unique ID.
        /// </summary>
        /// <param name="id">The ingredient's unique identifier.</param>
        /// <returns>The ingredient DTO if found, otherwise null.</returns>
        Task<IngredientDto?> GetIngredientByIdAsync(int id);
        /// <summary>
        /// Retrieves all ingredients.
        /// </summary>
        /// <returns>An enumerable collection of ingredient DTOs.</returns>
        Task<IEnumerable<IngredientDto>> GetAllIngredientsAsync();
        /// <summary>
        /// Creates a new ingredient.
        /// </summary>
        /// <param name="ingredientDto">The ingredient DTO to create.</param>
        /// <returns>The created ingredient DTO.</returns>
        Task<IngredientDto> CreateIngredientAsync(IngredientDto ingredientDto);
        /// <summary>
        /// Updates an existing ingredient.
        /// </summary>
        /// <param name="ingredientDto">The ingredient DTO to update.</param>
        /// <returns>The updated ingredient DTO.</returns>
        Task<IngredientDto> UpdateIngredientAsync(IngredientDto ingredientDto);
        /// <summary>
        /// Deletes an ingredient by its unique ID.
        /// </summary>
        /// <param name="id">The ingredient's unique identifier.</param>
        /// <returns>True if the ingredient was deleted, otherwise false.</returns>
        Task<bool> DeleteIngredientAsync(int id);
    }
}
