using Backend.DTOs;

namespace Backend.Services
{
    public interface IRecipeService
    {
        /// <summary>
        /// Retrieves a recipe by its unique ID.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <returns>The recipe DTO if found, otherwise null.</returns>
        Task<RecipeDto?> GetRecipeByIdAsync(int id);
        /// <summary>
        /// Retrieves all recipes.
        /// </summary>
        /// <returns>An enumerable collection of recipe DTOs.</returns>
        Task<IEnumerable<RecipeDto>> GetAllRecipesAsync();
        /// <summary>
        /// Creates a new recipe.
        /// </summary>
        /// <param name="recipeDto">The recipe DTO to create.</param>
        /// <returns>The created recipe DTO.</returns>
        Task<RecipeDto> CreateRecipeAsync(RecipeDto recipeDto);
        /// <summary>
        /// Updates an existing recipe.
        /// </summary>
        /// <param name="recipeDto">The recipe DTO to update.</param>
        /// <returns>The updated recipe DTO.</returns>
        Task<RecipeDto> UpdateRecipeAsync(RecipeDto recipeDto);
        /// <summary>
        /// Deletes a recipe by its unique ID.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <returns>True if the recipe was deleted, otherwise false.</returns>
        Task<bool> DeleteRecipeAsync(int id);
    }
}
