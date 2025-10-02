using System.ComponentModel.DataAnnotations;
using System.Security;
using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services.Impl
{
    public class MealPlanService(PlannerContext context, ILogger<MealPlanService> logger, IMealPlanGenerator recipeGenerator) : IMealPlanService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<MealPlanService> _logger = logger;
        private readonly IMealPlanGenerator _recipeGeneratorService = recipeGenerator;

        // this might be the only place where I don't eagerly load the whole object graph
        // because meal plans can have many meals and recipes can have many ingredients
        public async Task<GetMealPlansResult> GetMealPlansAsync(int userId, int? skip, int? take, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering GetMealPlansAsync: userId={UserId}, skip={Skip}, take={Take}", userId, skip, take);

            if (skip != null && skip < 0)
            {
                _logger.LogWarning("GetMealPlansAsync: Skip must be non-negative. skip={Skip}", skip);
                throw new ArgumentException("Skip must be non-negative.");
            }

            if (take != null && take <= 0)
            {
                _logger.LogWarning("GetMealPlansAsync: Take must be positive. take={Take}", take);
                throw new ArgumentException("Take must be positive.");
            }

            if (await _context.Users.FirstOrDefaultAsync(u => u.Id == userId, ct) == null)
            {
                _logger.LogWarning("GetMealPlansAsync: Attempting to get meal plans for non-existent user. userId={UserId}", userId);
                throw new ValidationException("Attempting to get meal plans for non-existent user.");
            }

            var query = _context.MealPlans
                .AsNoTracking()
                .Include(m => m.Meals)
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.StartDate)
                .AsQueryable();

            if (skip != null)
                query = query.Skip((int)skip);

            if (take != null)
                query = query.Take((int)take);

            var plans = await query.ToListAsync(ct);
            var count = await _context.MealPlans.CountAsync(ct);

            _logger.LogInformation("GetMealPlansAsync: Retrieved {Count} meal plans for userId={UserId}", plans.Count, userId);
            _logger.LogInformation("Exiting GetMealPlansAsync: userId={UserId}", userId);

            return new GetMealPlansResult { TotalCount = count, MealPlans = plans.Select(p => p.ToDto()) };
        }

        public async Task<MealPlanDto> AddMealPlanAsync(int userId, CreateUpdateMealPlanRequestDto request, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering AddMealPlanAsync: userId={UserId}", userId);
            var user = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(ct);
            if (user == null)
            {
                _logger.LogWarning("AddMealPlanAsync: Attempting to create meal plan for non-existent user. userId={UserId}", userId);
                throw new ArgumentException("Attempting to create meal plan for non-existent user.");
            }

            if (request.Meals.IsNullOrEmpty())
            {
                _logger.LogWarning("AddMealPlanAsync: Cannot create meal plan with no meals. userId={UserId}", userId);
                throw new ArgumentException("Cannot create meal plan with no meals.");
            }

            // verify recipe ids before creating meal plan entries
            var recipeIds = request.Meals.Where(m => m.RecipeId != null).Select(m => m.RecipeId).ToHashSet();
            var recipeDbCount = await _context.Recipes.Where(r => r.UserId == userId && recipeIds.Contains(r.Id)).CountAsync(ct);
            if (recipeIds.Count != recipeDbCount)
            {
                _logger.LogWarning("Invalid RecipeIds were passed in.");
                throw new ArgumentException("Not all given RecipeIds were valid.");
            }

            var newMeals = request.Meals.Select(m => new MealPlanEntry { Notes = m.Notes, RecipeId = m.RecipeId });
            var newMealPlan = new MealPlan { StartDate = request.StartDate, UserId = userId, Meals = [.. newMeals] };

            await _context.MealPlans.AddAsync(newMealPlan, ct);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("AddMealPlanAsync: Meal plan created for userId={UserId}, mealPlanId={MealPlanId}", userId, newMealPlan.Id);
            _logger.LogInformation("Exiting AddMealPlanAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, newMealPlan.Id);
            return newMealPlan.ToDto();
        }

        public async Task<MealPlanDto> UpdateMealPlanAsync(int id, int userId, CreateUpdateMealPlanRequestDto request, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering UpdateMealPlanAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, id);
            var user = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(ct);
            if (user == null)
            {
                _logger.LogWarning("UpdateMealPlanAsync: Attempting to update meal plan for non-existent user. userId={UserId}", userId);
                throw new ArgumentException("Attempting to create meal pln for non-existent user.");
            }

            var mealPlan = await _context.MealPlans
                .Include(m => m.Meals)
                .Where(m => m.Id == id && m.UserId == userId)
                .FirstOrDefaultAsync(ct);

            if (mealPlan == null)
            {
                _logger.LogWarning("UpdateMealPlanAsync: Cannot find meal plan to update. userId={UserId}, mealPlanId={MealPlanId}", userId, id);
                throw new SecurityException("Cannot find meal plan to update.");
            }

            if (request.Id != null && request.Id != id)
            {
                _logger.LogWarning("UpdateMealPlanAsync: Attempting to update a different meal plan than the one defined by the id. userId={UserId}, mealPlanId={MealPlanId}", userId, id);
                throw new ArgumentException("Attempting to update a different meal plan than the one defined by the id.");
            }

            mealPlan.StartDate = request.StartDate;

            // We remove the meals in the meal plan object that aren't in the request list
            // If the meals match the id, then we update the meal in the meal plan object.
            // We add any new meals still left in the request object

            // Remove deleted
            // if id is null then it is a new meal and it will be created below
            var mealsList = request.Meals.ToList();
            var dtoIds = mealsList.Where(m => m.Id != null).Select(m => m.Id!.Value).ToHashSet();

            //find the meals that are in the meal plan but not in the dto
            var toRemove = mealPlan.Meals.Where(m => !dtoIds.Contains(m.Id)).ToList();
            _context.MealPlanEntries.RemoveRange(toRemove);

            // Verify all recipe ids from the dto exist
            var recipeDtoIds = mealsList.Where(m => m.RecipeId != null).Select(m => m.RecipeId!.Value).ToHashSet();
            if (await _context.Recipes.Where(r => r.UserId == userId && recipeDtoIds.Contains(r.Id)).CountAsync(ct) != recipeDtoIds.Count)
                throw new ArgumentException("Attempting to update meal plan with non-existent recipe.");

            foreach (var mealDto in mealsList)
            {
                var existingEntry = mealPlan.Meals
                    .FirstOrDefault(m => m.Id == mealDto.Id);

                if (existingEntry != null)
                {
                    // Update existing
                    existingEntry.Notes = mealDto.Notes;
                    existingEntry.RecipeId = mealDto.RecipeId;
                }
                else
                {
                    // Add new
                    mealPlan.Meals.Add(new MealPlanEntry
                    {
                        Notes = mealDto.Notes,
                        RecipeId = mealDto.RecipeId
                    });
                }
            }

            await _context.SaveChangesAsync(ct);
            _logger.LogInformation("UpdateMealPlanAsync: Meal plan updated for userId={UserId}, mealPlanId={MealPlanId}", userId, id);
            _logger.LogInformation("Exiting UpdateMealPlanAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, id);
            return mealPlan.ToDto();
        }

        public async Task<bool> DeleteMealPlanAsync(int id, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering DeleteMealPlanAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, id);

            var user = await _context.Users.Where(u => u.Id == userId).FirstOrDefaultAsync(ct);
            if (user == null)
            {
                _logger.LogWarning("DeleteMealPlanAsync: Attempting to delete meal plan for non-existent user. userId={UserId}", userId);
                throw new ArgumentException("Attempting to delete meal plan for non-existent user.");
            }

            var mealPlan = await _context.MealPlans.Where(m => m.Id == id && m.UserId == userId).FirstOrDefaultAsync(ct);
            if (mealPlan == null)
            {
                _logger.LogWarning("DeleteMealPlanAsync: Attempting to delete non-existent meal plan. userId={UserId}, mealPlanId={MealPlanId}", userId, id);
                throw new SecurityException("Attempting to delete non-existent meal plan.");
            }

            _context.MealPlans.Remove(mealPlan);
            var deleted = await _context.SaveChangesAsync(ct);

            _logger.LogInformation("DeleteMealPlanAsync: Meal plan successfully removed. userId={UserId}, mealPlanId={MealPlanId}", userId, id);
            _logger.LogInformation("Exiting DeleteMealPlanAsync: userId={UserId}, mealPlanId={MealPlanId}", userId, id);

            return deleted > 0;
        }

        //here, too, we don't load the whole object graph. Recipes are not eagerly loaded
        //because meal plans can have many meals and recipes can have many ingredients
        public async Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(GenerateMealPlanRequestDto request, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering GenerateMealPlanAsync: userId={UserId}, days={Days}, useExternal={UseExternal}", userId, request.Days, request.UseExternal);
            if (request.Days <= 0)
            {
                // should be validated in controller, but just in case
                _logger.LogWarning("GenerateMealPlanAsync: Days must be positive. days={Days}", request.Days);
                throw new ArgumentException("Days must be positive.");
            }

            var mealPlan = await _recipeGeneratorService.GenerateMealPlanAsync(request.Days, userId, request.UseExternal, ct);
            mealPlan.StartDate = request.StartDate;

            _logger.LogInformation("GenerateMealPlanAsync: Meal plan successfully generated for userId={UserId}", userId);
            _logger.LogInformation("Exiting GenerateMealPlanAsync: userId={UserId}", userId);

            return mealPlan;
        }

        public async Task<GetPantryItemsResult> CookMealAsync(int id, int mealEntryId, int userId, CancellationToken ct = default)
        {
            _logger.LogInformation("Entering CookMealAsync: userId={UserId}, mealPlanId={MealPlanId}, mealEntryId={MealEntryId}", userId, id, mealEntryId);
            var mealPlan = await _context.MealPlans
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id && m.UserId == userId, ct);

            if (mealPlan == null)
            {
                _logger.LogWarning("CookMealAsync: Valid meal plan id required. userId={UserId}, mealPlanId={MealPlanId}", userId, id);
                throw new SecurityException("Valid meal plan id required.");
            }

            var mealPlanEntry = await _context.MealPlanEntries
                .Include(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .FirstOrDefaultAsync(m => m.Id == mealEntryId && m.MealPlanId == id, ct);

            if (mealPlanEntry == null)
            {
                _logger.LogWarning("CookMealAsync: Valid meal plan entry id required. userId={UserId}, mealPlanId={MealPlanId}, mealEntryId={MealEntryId}", userId, id, mealEntryId);
                throw new SecurityException("Valid meal plan entry id required.");
            }

            // mark the meal as cooked
            // this is idempotent, so cooking a meal twice is not an error
            mealPlanEntry.Cooked = true;

            // we allow meal plans with just notes, so recipe can be null
            if (mealPlanEntry.Recipe == null)
            {
                _logger.LogInformation("CookMealAsync: Meal plan entry had no recipe, so returning early.");
                _logger.LogInformation("Exiting CookMealAsync: userId={UserId}, mealPlanId={MealPlanId}, mealEntryId={MealEntryId}", userId, id, mealEntryId);
                return new GetPantryItemsResult { TotalCount = 0, Items = [] };
            }

            // find all pantry items that match the food ids in the recipe
            // we don't reduce the quantity here, just return the items that were used
            var pantry = await _context.PantryItems
                .AsNoTracking()
                .Include(p => p.Food)
                .ThenInclude(f => f.Category)
                .Where(p => p.UserId == userId)
                .ToListAsync(ct);

            var recipeFoodIds = mealPlanEntry.Recipe.Ingredients.Select(i => i.FoodId).ToHashSet();
            var usedPantryItems = pantry.Where(p => recipeFoodIds.Contains(p.FoodId));
            var items = usedPantryItems.Select(p => p.ToDto());

            _logger.LogInformation("CookMealAsync: Cooked meal for userId={UserId}, mealPlanId={MealPlanId}, mealEntryId={MealEntryId}", userId, id, mealEntryId);
            _logger.LogInformation("Exiting CookMealAsync: userId={UserId}, mealPlanId={MealPlanId}, mealEntryId={MealEntryId}", userId, id, mealEntryId);

            //we save after our logic so that in case something goes with pulling pantry items, the recipe doesn't get marked as cooked
            await _context.SaveChangesAsync(ct);
            return new GetPantryItemsResult { TotalCount = usedPantryItems.Count(), Items = items };
        }
    }
}
