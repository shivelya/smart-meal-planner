using Backend.DTOs;

namespace Backend.Services
{
    public interface IRecipeIngredientService
    {
        /// <summary>
        /// Retrieves a recipe ingredient by recipe and ingredient IDs.
        /// </summary>
        /// <param name="recipeId">The recipe's unique identifier.</param>
        /// <param name="ingredientId">The ingredient's unique identifier.</param>
        /// <returns>The recipe ingredient DTO if found, otherwise null.</returns>
        Task<RecipeIngredientDto?> GetRecipeIngredientAsync(int recipeId, int ingredientId);
        /// <summary>
        /// Retrieves all recipe ingredients.
        /// </summary>
        /// <returns>An enumerable collection of recipe ingredient DTOs.</returns>
        Task<IEnumerable<RecipeIngredientDto>> GetAllRecipeIngredientsAsync();
        /// <summary>
        /// Creates a new recipe ingredient.
        /// </summary>
        /// <param name="recipeIngredientDto">The recipe ingredient DTO to create.</param>
        /// <returns>The created recipe ingredient DTO.</returns>
        Task<RecipeIngredientDto> CreateRecipeIngredientAsync(RecipeIngredientDto recipeIngredientDto);
        /// <summary>
        /// Updates an existing recipe ingredient.
        /// </summary>
        /// <param name="recipeIngredientDto">The recipe ingredient DTO to update.</param>
        /// <returns>The updated recipe ingredient DTO.</returns>
        Task<RecipeIngredientDto> UpdateRecipeIngredientAsync(RecipeIngredientDto recipeIngredientDto);
        /// <summary>
        /// Deletes a recipe ingredient by recipe and ingredient IDs.
        /// </summary>
        /// <param name="recipeId">The recipe's unique identifier.</param>
        /// <param name="ingredientId">The ingredient's unique identifier.</param>
        /// <returns>True if the recipe ingredient was deleted, otherwise false.</returns>
        Task<bool> DeleteRecipeIngredientAsync(int recipeId, int ingredientId);
    }
}
