using System.ComponentModel.DataAnnotations;
using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class RecipeService(PlannerContext context, ILogger<RecipeService> logger) : IRecipeService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<RecipeService> _logger = logger;

        /// <summary>
        /// Creates a new recipe for the specified user.
        /// </summary>
        /// <param name="request">The DTO containing recipe details.</param>
        /// <param name="userId">The user ID to associate with the recipe.</param>
        /// <returns>The created recipe DTO.</returns>
        /// <exception cref="ValidationException">Thrown when required fields are missing or ingredient type is unknown.</exception>
        public async Task<RecipeDto> CreateAsync(CreateRecipeDtoRequest request, int userId)
        {
            _logger.LogInformation("Creating recipe for user {UserId}: {@Request}", userId, request);
            Recipe recipe = new() { UserId = userId };

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions) || request.Ingredients == null || request.Ingredients.Count == 0)
            {
                _logger.LogWarning("Title, instructions, and at least one ingredient are required to create recipe.");
                throw new ValidationException("Title, instructions, and at least one ingredient are required to create recipe.");
            }

            recipe.Title = request.Title;
            recipe.Instructions = request.Instructions;
            recipe.Source = request.Source;
            recipe.Ingredients = await CreateIngredients(request.Ingredients);

            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe created with ID {Id}", recipe.Id);
            return recipe.ToDto();
        }

        /// <summary>
        /// Deletes a recipe by its unique ID for the specified user.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="userId">The user ID who owns the recipe.</param>
        /// <returns>True if the recipe was deleted, otherwise false.</returns>
        /// <exception cref="ArgumentException">Thrown when the recipe is not found or user does not have permission.</exception>
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            _logger.LogInformation("Deleting recipe with ID {Id} for user {UserId}", id, userId);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id);
            if (recipe is null)
            {
                _logger.LogWarning("No such ingredient found for recipe ID {Id}", id);
                throw new ArgumentException("No such ingredient found.");
            }
            if (recipe.UserId != userId)
            {
                _logger.LogWarning("User {UserId} does not have permission to delete recipe {Id}", userId, id);
                throw new ArgumentException("User does not have permission to delete ingredient.");
            }
            _context.Recipes.Remove(recipe);
            int num = await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe with ID {Id} deleted", id);
            return num > 0;
        }

        /// <summary>
        /// Retrieves a recipe by its unique ID for the specified user.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="userId">The user ID who owns the recipe.</param>
        /// <returns>The recipe DTO if found, otherwise null.</returns>
        public async Task<RecipeDto?> GetByIdAsync(int id, int userId)
        {
            _logger.LogInformation("Retrieving recipe with ID {Id} for user {UserId}", id, userId);
            var entity = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (entity is null)
            {
                _logger.LogWarning("Recipe with ID {Id} not found for user {UserId}", id, userId);
                return null;
            }
            _logger.LogInformation("Recipe retrieved: {@Recipe}", entity);
            return entity.ToDto();
        }

        /// <summary>
        /// Retrieves multiple recipes by their IDs for the specified user.
        /// </summary>
        /// <param name="ids">A collection of recipe IDs to retrieve.</param>
        /// <param name="userId">The user ID who owns the recipes.</param>
        /// <returns>A collection of recipe DTOs.</returns>
        public async Task<IEnumerable<RecipeDto>> GetByIdsAsync(IEnumerable<int> ids, int userId)
        {
            var idSet = ids.Distinct().ToList();
            _logger.LogInformation("Retrieving recipes with IDs {@Ids} for user {UserId}", idSet, userId);
            if (idSet.Count == 0)
            {
                _logger.LogWarning("No IDs provided for GetByIds");
                return Array.Empty<RecipeDto>();
            }
            var entities = await _context.Recipes
                .Include(r => r.Ingredients)
                .Where(r => r.UserId == userId && idSet.Contains(r.Id))
                .ToListAsync();
            _logger.LogInformation("Retrieved {Count} recipes", entities.Count);
            return [.. entities.Select(r => r.ToDto())];
        }

        /// <summary>
        /// Searches for recipes based on title and/or ingredient for the specified user.
        /// </summary>
        /// <param name="options">Search options including title, ingredient, skip, and take.</param>
        /// <param name="userId">The user ID who owns the recipes.</param>
        /// <returns>A collection of recipe DTOs matching the search criteria.</returns>
        public async Task<IEnumerable<RecipeDto>> SearchAsync(RecipeSearchOptions options, int userId)
        {
            _logger.LogInformation("Searching recipes for user {UserId}: {@Options}", userId, options);
            if (options.Take is <= 0 or > 200)
            {
                options.Take = 50;
                _logger.LogWarning("Resetting the Take to 50 because it was negative or over 200");
            }

            if (options.Skip is < 0)
            {
                options.Skip = 0;
                _logger.LogWarning("Resetting the Skip because it was negative.");
            }

            var query = _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Where(r => r.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(options.TitleContains))
            {
                var title = options.TitleContains.Trim();
                var pattern = $"%{title}%";
                query = query.Where(r => r.Title != null && r.Title!.Contains(title, StringComparison.CurrentCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(options.IngredientContains))
            {
                var ing = options.IngredientContains.Trim();
                var pattern = $"%{ing}%";
                query = query.Where(r => r.Ingredients.Any(i => i.Food.Name.Contains(ing, StringComparison.CurrentCultureIgnoreCase)));
            }

            if (options.Skip is > 0) query = query.Skip(options.Skip!.Value);
            if (options.Take is > 0) query = query.Take(options.Take!.Value);

            var list = await query.ToListAsync();
            _logger.LogInformation("Search returned {Count} recipes", list.Count);
            return [.. list.Select(r => r.ToDto())];
        }

        /// <summary>
        /// Updates an existing recipe for the specified user.
        /// </summary>
        /// <param name="id">The recipe's unique identifier.</param>
        /// <param name="recipeDto">The DTO containing updated recipe details.</param>
        /// <param name="userId">The user ID who owns the recipe.</param>
        /// <returns>The updated recipe DTO.</returns>
        /// <exception cref="ArgumentException">Thrown when the recipe is not found.</exception>
        /// <exception cref="ValidationException">Thrown when the user does not have permission to update the recipe.</exception>
        public async Task<RecipeDto> UpdateAsync(int id, CreateRecipeDtoRequest recipeDto, int userId)
        {
            _logger.LogInformation("Updating recipe with ID {Id} for user {UserId}: {@RecipeDto}", id, userId, recipeDto);
            var entity = await _context.Recipes
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new ArgumentException("Recipe with id {0} could not be found.", id.ToString());

            if (entity.UserId != userId)
            {
                _logger.LogWarning("User {UserId} does not have permission to update recipe {Id}", userId, id);
                throw new ValidationException("Do not have permission to update this recipe.");
            }
            entity.Title = recipeDto.Title;
            entity.Instructions = recipeDto.Instructions;
            entity.Source = recipeDto.Source;

            // Replace all ingredients with the provided set
            entity.Ingredients.Clear();
            foreach (var ing in await CreateIngredients(recipeDto.Ingredients))
                entity.Ingredients.Add(ing);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe with ID {Id} updated", id);
            return entity.ToDto();
        }

        private void ValidateIngredient(RecipeIngredientDto ing)
        {
            if (ing.Food.Mode == AddFoodMode.Existing)
            {
                if (_context.Foods.FirstOrDefaultAsync(i => i.Id == ing.Food.Id) == null)
                {
                    _logger.LogWarning("Found ingredient with unknown ID.");
                    throw new ValidationException("Found ingredient with unknown ID.");
                }
            }
            else if ( ing.Food.Mode == AddFoodMode.New)
            {
                if (_context.Categories.FirstOrDefaultAsync(i => i.Id == ing.Food.CategoryId) == null)
                {
                    _logger.LogWarning("Found ingredient with unknown category.");
                    throw new ValidationException("Found ingredient with unknown category.");
                }

                if (string.IsNullOrWhiteSpace(ing.Food.Name))
                {
                    _logger.LogWarning("Ingredient name required.");
                    throw new ValidationException("Ingredient name required.");
                }
            }
        }

        private async Task<List<RecipeIngredient>> CreateIngredients(List<RecipeIngredientDto> ingredients)
        {
            var toReturn = new List<RecipeIngredient>();
            foreach (var ing in ingredients)
            {
                ValidateIngredient(ing);
                var recipeIngredient = new RecipeIngredient
                {
                    Quantity = ing.Quantity,
                    Unit = ing.Unit
                };
                if (ing.Food.Mode == AddFoodMode.Existing)
                {
                    //is pre-existing ingredient, just need to create corresponding RecipeIngredient
                    recipeIngredient.FoodId = ing.Food.Id!.Value;
                }
                else if (ing.Food.Mode == AddFoodMode.New)
                {
                    // this is a new ingredient, need to create the ingredient before creating the recipe ingredient
                    var ingredient = new Food
                    {
                        Name = ing.Food.Name!,
                        CategoryId = ing.Food.CategoryId!.Value
                    };

                    await _context.Foods.AddAsync(ingredient);
                    await _context.SaveChangesAsync();

                    recipeIngredient.FoodId = ingredient.Id;
                }
                else
                {
                    _logger.LogError("Unknown ingredient type found.");
                }

                toReturn.Add(recipeIngredient);
            }

            return toReturn;
        }
    }
}