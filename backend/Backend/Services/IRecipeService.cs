using Backend.DTOs;
using Backend.Model;

namespace Backend.Services
{
    public interface IRecipeService
    {
        /// <summary>
        /// Creates a new recipe.
        /// </summary>
        /// <param name="recipeDto">The recipe DTO to create.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>The created recipe DTO.</returns>
        Task<RecipeDto> CreateAsync(CreateUpdateRecipeDtoRequest recipeDto, int userId);
        /// <summary>
        /// Retrieves a recipe by its unique ID.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>The recipe DTO if found, otherwise null.</returns>
        Task<RecipeDto?> GetByIdAsync(int id, int userId);
        /// <summary>
        /// Retrieves all recipes.
        /// </summary>
        /// <param name="ids">A list of recipe's unique identifiers.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>An enumerable collection of recipe DTOs.</returns>
        Task<GetRecipesResult> GetByIdsAsync(IEnumerable<int> ids, int userId);
        /// <summary>
        /// Searches for recipes by title or by ingredient.
        /// </summary>
        /// <param name="userId">The currently logged in user.</param>
        /// <param name="title">Search term to use on recipe titles.</param>
        /// <param name="ingredient">Search term to use on recipe ingredients.</param>
        /// <param name="skip">How many results to skip for pagination.</param>
        /// <param name="take">How many results to take for pagniation.</param>
        /// <returns>The matching recipe ids.</returns>
        Task<GetRecipesResult> SearchAsync(int userId, string? title, string? ingredient, int? skip = 0, int? take = 50);
        /// <summary>
        /// Updates an existing recipe.
        /// </summary>
        /// <param name="id">The id of the recipe to update.</param>
        /// <param name="recipeDto">The recipe DTO to update.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>The updated recipe DTO.</returns>
        Task<RecipeDto> UpdateAsync(int id, CreateUpdateRecipeDtoRequest recipeDto, int userId);
        /// <summary>
        /// Deletes a recipe by its unique ID.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="userId">The id of the currently logged in user.</param>
        /// <returns>True if the recipe was deleted, otherwise false.</returns>
        Task<bool> DeleteAsync(int id, int userId);
        /// <summary>
        /// Finds pantry items used while making this recipe.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="userId">The id of the logged in user.</param>
        /// <returns>A list of ingredients possibly used up while making the recipe.</returns>
        GetPantryItemsResult CookRecipe(int id, int userId);
    }
}
