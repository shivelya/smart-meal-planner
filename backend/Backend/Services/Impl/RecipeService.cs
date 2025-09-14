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

        public async Task<RecipeDto> CreateAsync(CreateUpdateRecipeDtoRequest request, int userId)
        {
            _logger.LogInformation("Creating recipe for user {UserId}: {@Request}", userId, request);

            if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Instructions) || request.Ingredients == null || request.Ingredients.Count == 0)
            {
                _logger.LogWarning("Title, instructions, and at least one ingredient are required to create recipe.");
                throw new ValidationException("Title, instructions, and at least one ingredient are required to create recipe.");
            }

            if (await _context.Users.FindAsync(userId) == null)
            {
                _logger.LogWarning("User ID {UserId} does not exist.", userId);
                throw new ValidationException("User ID does not exist.");
            }

            Recipe recipe = new()
            {
                UserId = userId,
                Title = request.Title,
                Instructions = request.Instructions,
                Source = request.Source,
                Ingredients = await CreateIngredients(request.Ingredients)
            };

            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe created with ID {Id}", recipe.Id);
            return recipe.ToDto();
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            _logger.LogInformation("Deleting recipe with ID {Id} for user {UserId}", id, userId);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);
            if (recipe is null)
            {
                _logger.LogWarning("No such ingredient found for recipe ID {Id}", id);
                throw new ArgumentException("No such ingredient found.");
            }

            _context.Recipes.Remove(recipe);
            int num = await _context.SaveChangesAsync();
            _logger.LogInformation("Recipe with ID {Id} deleted", id);
            return num > 0;
        }

        public async Task<RecipeDto?> GetByIdAsync(int id, int userId)
        {
            _logger.LogInformation("Retrieving recipe with ID {Id} for user {UserId}", id, userId);
            var entity = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId);

            if (entity is null)
            {
                _logger.LogWarning("Recipe with ID {Id} not found for user {UserId}", id, userId);
                return null;
            }

            _logger.LogInformation("Recipe retrieved: {@Recipe}", entity);
            return entity.ToDto();
        }

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
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
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
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
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

        public async Task<RecipeDto> UpdateAsync(int id, CreateUpdateRecipeDtoRequest recipeDto, int userId)
        {
            _logger.LogInformation("Updating recipe with ID {Id} for user {UserId}: {@RecipeDto}", id, userId, recipeDto);
            var entity = await _context.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
                ?? throw new ArgumentException("Recipe with id {0} could not be found.", id.ToString());

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
            var recipe = _context.Recipes.AsNoTracking().Include(r => r.Ingredients).FirstOrDefault(r => r.Id == id && r.UserId == userId)
                ?? throw new ArgumentException("id is not a valid id of a recipe.");

            var pantry = _context.PantryItems
                .AsNoTracking()
                .Include(p => p.Food)
                .ThenInclude(f => f.Category)
                .Where(p => p.UserId == userId)
                .ToList();
            var recipeFoodIds = recipe.Ingredients.Select(i => i.FoodId).ToList();
            var usedPantryItems = pantry.Where(p => recipeFoodIds.Contains(p.FoodId)).ToList();

            // don't actually modify the database, just return the items that would be used. They need to be verified by the user first.
            _logger.LogInformation("Recipe with ID {Id} cooked for user {UserId}, using {Count} pantry items", id, userId, usedPantryItems.Count);
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