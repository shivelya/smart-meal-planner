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
        public async Task<RecipeDto> CreateAsync(CreateUpdateRecipeDtoRequest request, int userId)
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
        public async Task<GetRecipesResult> GetByIdsAsync(IEnumerable<int> ids, int userId)
        {
            var idSet = ids.Distinct().ToList();
            _logger.LogInformation("Retrieving recipes with IDs {@Ids} for user {UserId}", idSet, userId);
            if (idSet.Count == 0)
            {
                _logger.LogWarning("No IDs provided for GetByIds");
                return new GetRecipesResult { TotalCount = 0, Items = []};
            }

            var entities = await _context.Recipes
                .Include(r => r.Ingredients)
                .Where(r => r.UserId == userId && idSet.Contains(r.Id))
                .ToListAsync();
            _logger.LogInformation("Retrieved {Count} recipes", entities.Count);
            return new GetRecipesResult { TotalCount = entities.Count, Items = [.. entities.Select(r => r.ToDto())] };
        }

        public async Task<GetRecipesResult> SearchAsync(int userId, string ? title, string ? ingredient, int? skip, int? take)
        {
            _logger.LogInformation("Searching recipes for user {UserId}: {Title}, {Ingredient}, {skip}, {take}", userId, title, ingredient, skip, take);

            var query = _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .Where(r => r.UserId == userId)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
            {
                title = title.Trim();
                var pattern = $"%{title}%";
                query = query.Where(r => r.Title != null && r.Title!.Contains(title, StringComparison.CurrentCultureIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(ingredient))
            {
                var ing = ingredient.Trim();
                var pattern = $"%{ing}%";
                query = query.Where(r => r.Ingredients.Any(i => i.Food.Name.Contains(ing, StringComparison.CurrentCultureIgnoreCase)));
            }

            var count = await query.CountAsync();

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("Negative skip used for search.");
                    throw new ArgumentException("Skip must be non-negative");
                }

                query = query.Skip(skip!.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("Negative take used for search.");
                    throw new ArgumentException("Take must be non-negative.");
                }

                query = query.Take(take!.Value);
            }

            var list = await query.ToListAsync();
            _logger.LogInformation("Search returned {Count} recipes", list.Count);
            return new GetRecipesResult { TotalCount = count, Items = [.. list.Select(r => r.ToDto())] };
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
        public async Task<RecipeDto> UpdateAsync(int id, CreateUpdateRecipeDtoRequest recipeDto, int userId)
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

        public GetPantryItemsResult CookRecipe(int id, int userId)
        {
            var recipe = _context.Recipes.AsNoTracking().Include(r => r.Ingredients).FirstOrDefault(r => r.Id == id)
                ?? throw new ArgumentException("id is not a valid id of a recipe.");

            if (recipe.UserId != userId)
                throw new ValidationException("User does not have permission to access this recipe.");

            var pantry = _context.PantryItems.Where(p => p.UserId == userId).ToList();
            var recipeFoodIds = recipe.Ingredients.Select(i => i.FoodId).ToList();
            var usedPantryItems = pantry.Where(p => recipeFoodIds.Contains(p.FoodId)).ToList();

            return new GetPantryItemsResult { TotalCount = usedPantryItems.Count, Items = usedPantryItems.Select(p => p.ToDto()) };
        }

        private void ValidateIngredient(CreateUpdateRecipeIngredientDto ing)
        {
            if (ing.Food.Mode == AddFoodMode.Existing)
            {
                var food = (ExistingFoodReferenceDto)ing.Food;
                if (_context.Foods.FirstOrDefaultAsync(i => i.Id == food.Id) == null)
                {
                    _logger.LogWarning("Found ingredient with unknown ID.");
                    throw new ValidationException("Found ingredient with unknown ID.");
                }
            }
            else if (ing.Food.Mode == AddFoodMode.New)
            {
                var food1 = (NewFoodReferenceDto)ing.Food;
                if (_context.Categories.FirstOrDefaultAsync(i => i.Id == food1.CategoryId) == null)
                {
                    _logger.LogWarning("Found ingredient with unknown category.");
                    throw new ValidationException("Found ingredient with unknown category.");
                }

                if (string.IsNullOrWhiteSpace(food1.Name))
                {
                    _logger.LogWarning("Ingredient name required.");
                    throw new ValidationException("Ingredient name required.");
                }
            }
        }

        private async Task<List<RecipeIngredient>> CreateIngredients(List<CreateUpdateRecipeIngredientDto> ingredients)
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
                    recipeIngredient.FoodId = ((ExistingFoodReferenceDto)ing.Food).Id;
                }
                else if (ing.Food.Mode == AddFoodMode.New)
                {
                    // this is a new ingredient, need to create the ingredient before creating the recipe ingredient
                    var food = (NewFoodReferenceDto)ing.Food;
                    var ingredient = new Food
                    {
                        Name = food.Name,
                        CategoryId = food.CategoryId
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