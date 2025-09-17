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

        public async Task<RecipeDto> CreateAsync(CreateUpdateRecipeDtoRequest recipeDto, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering CreateAsync: userId={UserId}", userId);
            _logger.LogInformation("Creating recipe for user {UserId}: {@RecipeDto}", userId, recipeDto);
            if (string.IsNullOrWhiteSpace(recipeDto.Title) || string.IsNullOrWhiteSpace(recipeDto.Instructions) || recipeDto.Ingredients == null || recipeDto.Ingredients.Count == 0)
            {
                _logger.LogWarning("CreateAsync: Title, instructions, and at least one ingredient are required to create recipe.");
                throw new ValidationException("Title, instructions, and at least one ingredient are required to create recipe.");
            }

            if (await _context.Users.FindAsync([userId], ct) == null)
            {
                _logger.LogWarning("CreateAsync: User ID {UserId} does not exist.", userId);
                throw new ValidationException("User ID does not exist.");
            }

            Recipe recipe = new()
            {
                UserId = userId,
                Title = recipeDto.Title,
                Instructions = recipeDto.Instructions,
                Source = recipeDto.Source,
                Ingredients = await CreateIngredients(recipeDto.Ingredients)
            };

            await _context.Recipes.AddAsync(recipe, ct);
            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("CreateAsync: Recipe created with ID {Id}", recipe.Id);
            _logger.LogInformation("Exiting CreateAsync: userId={UserId}, recipeId={RecipeId}", userId, recipe.Id);
            return recipe.ToDto();
        }

        public async Task<bool> DeleteAsync(int id, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering DeleteAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            _logger.LogInformation("Deleting recipe with ID {Id} for user {UserId}", id, userId);
            var recipe = await _context.Recipes.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);
            if (recipe is null)
            {
                _logger.LogWarning("DeleteAsync: No such ingredient found for recipe ID {Id}", id);
                throw new ArgumentException("No such ingredient found.");
            }

            _context.Recipes.Remove(recipe);
            int num = await _context.SaveChangesAsync(ct);
            _logger.LogInformation("DeleteAsync: Recipe with ID {Id} deleted", id);
            _logger.LogInformation("Exiting DeleteAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            return num > 0;
        }

        public async Task<RecipeDto?> GetByIdAsync(int id, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering GetByIdAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            _logger.LogInformation("Retrieving recipe with ID {Id} for user {UserId}", id, userId);
            var entity = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

            if (entity is null)
            {
                _logger.LogWarning("GetByIdAsync: Recipe with ID {Id} not found for user {UserId}", id, userId);
                return null;
            }

            _logger.LogInformation("GetByIdAsync: Recipe retrieved: {@Recipe}", entity);
            _logger.LogInformation("Exiting GetByIdAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            return entity.ToDto();
        }

        public async Task<GetRecipesResult> GetByIdsAsync(IEnumerable<int> ids, int userId, CancellationToken ct = default)
        {
            var idSet = ids.Distinct().ToList();
            _logger.LogInformation("Entering GetByIdsAsync: userId={UserId}, recipeIds={RecipeIds}", userId, idSet);
            _logger.LogInformation("Retrieving recipes with IDs {@Ids} for user {UserId}", idSet, userId);
            if (idSet.Count == 0)
            {
                _logger.LogWarning("GetByIdsAsync: No IDs provided for GetByIds");
                return new GetRecipesResult { TotalCount = 0, Items = [] };
            }

            var entities = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
                .Where(r => r.UserId == userId && idSet.Contains(r.Id))
                .ToListAsync(ct);
            _logger.LogInformation("GetByIdsAsync: Retrieved {Count} recipes", entities.Count);
            _logger.LogInformation("Exiting GetByIdsAsync: userId={UserId}, recipeIds={RecipeIds}", userId, idSet);
            return new GetRecipesResult { TotalCount = entities.Count, Items = [.. entities.Select(r => r.ToDto())] };
        }

        public async Task<GetRecipesResult> SearchAsync(int userId, string? title, string? ingredient, int? skip, int? take, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering SearchAsync: userId={UserId}, title={Title}, ingredient={Ingredient}, skip={Skip}, take={Take}", userId, title, ingredient, skip, take);
            _logger.LogInformation("Searching recipes for user {UserId}: {Title}, {Ingredient}, {Skip}, {Take}", userId, title, ingredient, skip, take);

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

            var count = await query.CountAsync(ct);

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogWarning("SearchAsync: Negative skip used for search.");
                    throw new ArgumentException("Skip must be non-negative");
                }

                query = query.Skip(skip!.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogWarning("SearchAsync: Negative take used for search.");
                    throw new ArgumentException("Take must be non-negative.");
                }

                query = query.Take(take!.Value);
            }

            var list = await query.ToListAsync(ct);
            _logger.LogInformation("SearchAsync: Search returned {Count} recipes", list.Count);
            _logger.LogInformation("Exiting SearchAsync: userId={UserId}, title={Title}, ingredient={Ingredient}, skip={Skip}, take={Take}", userId, title, ingredient, skip, take);
            return new GetRecipesResult { TotalCount = count, Items = [.. list.Select(r => r.ToDto())] };
        }

        public async Task<RecipeDto> UpdateAsync(int id, CreateUpdateRecipeDtoRequest recipeDto, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering UpdateAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            _logger.LogInformation("Updating recipe with ID {Id} for user {UserId}: {@RecipeDto}", id, userId, recipeDto);
            var entity = await _context.Recipes
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .ThenInclude(f => f.Category)
                .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct)
                ?? throw new ArgumentException($"Recipe with id {id.ToString(System.Globalization.CultureInfo.InvariantCulture)} could not be found.");

            entity.Title = recipeDto.Title;
            entity.Instructions = recipeDto.Instructions;
            entity.Source = recipeDto.Source;

            // Replace all ingredients with the provided set
            entity.Ingredients.Clear();
            foreach (var ing in await CreateIngredients(recipeDto.Ingredients))
                entity.Ingredients.Add(ing);

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("UpdateAsync: Recipe with ID {Id} updated", id);
            _logger.LogInformation("Exiting UpdateAsync: userId={UserId}, recipeId={RecipeId}", userId, id);
            return entity.ToDto();
        }

        public GetPantryItemsResult CookRecipe(int id, int userId)
        {
            _logger.LogInformation("Entering CookRecipe: userId={UserId}, recipeId={RecipeId}", userId, id);
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
            _logger.LogInformation("CookRecipe: Recipe with ID {Id} cooked for user {UserId}, using {Count} pantry items", id, userId, usedPantryItems.Count);
            _logger.LogInformation("Exiting CookRecipe: userId={UserId}, recipeId={RecipeId}", userId, id);
            return new GetPantryItemsResult { TotalCount = usedPantryItems.Count, Items = usedPantryItems.Select(p => p.ToDto()) };
        }

        private void ValidateIngredient(CreateUpdateRecipeIngredientDto ing)
        {
            _logger.LogInformation("Entering ValidateIngredient: foodMode={FoodMode}", ing.Food.Mode);
            if (ing.Food.Mode == AddFoodMode.Existing)
            {
                var food = (ExistingFoodReferenceDto)ing.Food;
                if (_context.Foods.FirstOrDefaultAsync(i => i.Id == food.Id) == null)
                {
                    _logger.LogWarning("ValidateIngredient: Found ingredient with unknown ID.");
                    throw new ValidationException("Found ingredient with unknown ID.");
                }
            }
            else if (ing.Food.Mode == AddFoodMode.New)
            {
                var food1 = (NewFoodReferenceDto)ing.Food;
                if (_context.Categories.FirstOrDefaultAsync(i => i.Id == food1.CategoryId) == null)
                {
                    _logger.LogWarning("ValidateIngredient: Found ingredient with unknown category.");
                    throw new ValidationException("Found ingredient with unknown category.");
                }

                if (string.IsNullOrWhiteSpace(food1.Name))
                {
                    _logger.LogWarning("ValidateIngredient: Ingredient name required.");
                    throw new ValidationException("Ingredient name required.");
                }
            }
            _logger.LogInformation("Exiting ValidateIngredient: foodMode={FoodMode}", ing.Food.Mode);
        }

        // do not save here for transactional purposes
        private async Task<List<RecipeIngredient>> CreateIngredients(List<CreateUpdateRecipeIngredientDto> ingredients)
        {
            _logger.LogInformation("Entering CreateIngredients: ingredientCount={IngredientCount}", ingredients.Count);
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

                    recipeIngredient.FoodId = ingredient.Id;
                }
                else
                {
                    _logger.LogError("CreateIngredients: Unknown ingredient type found.");
                }

                toReturn.Add(recipeIngredient);
            }
            _logger.LogInformation("Exiting CreateIngredients: createdCount={CreatedCount}", toReturn.Count);
            return toReturn;
        }
    }
}