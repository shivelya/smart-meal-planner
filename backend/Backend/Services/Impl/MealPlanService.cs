using System.ComponentModel.DataAnnotations;
using System.Security;
using System.Threading.Tasks;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services.Impl
{
    public class MealPlanService(PlannerContext context, ILogger<MealPlanService> logger, IRecipeGenerator recipeGenerator) : IMealPlanService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<MealPlanService> _logger = logger;
        private readonly IRecipeGenerator _recipeGenerator = recipeGenerator;

        public async Task<GetMealPlansResult> GetMealPlansAsync(int? skip, int? take)
        {
            if (skip < 0)
            {
                _logger.LogWarning("Skip must be non-negative.");
                throw new ArgumentException("Skip must be non-negative.");
            }

            if (take <= 0)
            {
                _logger.LogWarning("Take must be positive.");
                throw new ArgumentException("Take must be positive.");
            }

            var query = _context.MealPlans.AsQueryable();
            if (skip != null)
                query = query.Skip((int)skip);

            if (take != null)
                query = query.Take((int)take);

            var plans = await query.ToListAsync();
            var count = await _context.MealPlans.CountAsync();

            return new GetMealPlansResult { TotalCount = count, MealPlans = plans.Select(p => p.ToDto()) };
        }

        public async Task<MealPlanDto> AddMealPlanAsync(int userId, CreateUpdateMealPlanRequestDto request)
        {
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning("Attempting to create meal plan for non-existent user.");
                throw new SecurityException("Attempting to create meal pln for non-existent user.");
            }

            if (request.Meals.IsNullOrEmpty())
            {
                _logger.LogWarning("Cannot create meal plan with no meals.");
                throw new ValidationException("Cannot create meal plan with no meals.");
            }

            var newMeals = request.Meals.Select(m => new MealPlanEntry { Notes = m.Notes, RecipeId = m.RecipeId });
            var newMealPlan = new MealPlan { StartDate = request.StartDate, UserId = userId, Meals = [.. newMeals] };
            _context.MealPlans.Add(newMealPlan);
            await _context.SaveChangesAsync();

            return newMealPlan.ToDto();
        }

        public async Task<MealPlanDto> UpdateMealPlanAsync(int id, int userId, CreateUpdateMealPlanRequestDto request)
        {
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning("Attempting to create meal plan for non-existent user.");
                throw new SecurityException("Attempting to create meal pln for non-existent user.");
            }

            var mealPlan = _context.MealPlans
                .Include(m => m.Meals)
                .Where(m => m.Id == id).FirstOrDefault();

            if (mealPlan == null)
            {
                _logger.LogWarning("Attempting to update non-existent meal plan.");
                throw new SecurityException("Attempting to update non-existent meal plan.");
            }

            if (request.Id != null && request.Id != id)
            {
                _logger.LogWarning("Attempting to update a different meal plan than the one defined by the id.");
                throw new SecurityException("Attempting to update a different meal plan than the one defined by the id.");
            }

            mealPlan.StartDate = request.StartDate;

            // We remove the meals in the meal plan object that aren't in the request list
            // If the meals match the id, then we update the meal in the meal plan object.
            // We add any new meals still left in the request object

            // Remove deleted
            // if id is null then it is a new meal and it will be created below
            var dtoIds = request.Meals.Where(m => m.Id != null).Select(m => m.Id).ToHashSet();
            var toRemove = mealPlan.Meals.Where(m => !dtoIds.Contains(m.Id)).ToList();

            foreach (var remove in toRemove)
                _context.MealPlanEntries.Remove(remove);

            foreach (var mealDto in request.Meals)
            {
                var existingEntry = mealPlan.Meals
                    .FirstOrDefault(m => m.Id == mealDto.Id);

                if (existingEntry != null)
                {
                    // Update existing
                    existingEntry.Notes = mealDto.Notes;
                    VerifyRecipe(mealDto.RecipeId, userId);
                    existingEntry.RecipeId = mealDto.RecipeId;
                }
                else
                {
                    // Add new
                    VerifyRecipe(mealDto.RecipeId, userId);
                    mealPlan.Meals.Add(new MealPlanEntry
                    {
                        Notes = mealDto.Notes,
                        RecipeId = mealDto.RecipeId
                    });
                }
            }

            await _context.SaveChangesAsync();
            return mealPlan.ToDto();
        }

        public async Task<bool> DeleteMealPlanAsync(int id, int userId)
        {
            var user = _context.Users.Where(u => u.Id == userId).FirstOrDefault();
            if (user == null)
            {
                _logger.LogWarning("Attempting to delete meal plan for non-existent user.");
                throw new SecurityException("Attempting to delete meal pln for non-existent user.");
            }

            var mealPlan = _context.MealPlans.Where(m => m.Id == id).FirstOrDefault();
            if (mealPlan == null)
            {
                _logger.LogWarning("Attempting to delete non-existent meal plan.");
                throw new ValidationException("Attemptint to delete non-existent meal plan.");
            }

            if (mealPlan.UserId != userId)
            {
                _logger.LogWarning("Attempting to delete meal plan that doesn't belong to user.");
                throw new ValidationException("Attempting to delete meal plan that doesn't belong to user.");
            }

            _context.MealPlans.Remove(mealPlan);
            var deleted = await _context.SaveChangesAsync();
            _logger.LogInformation("Meal plan successfully removed.");

            return deleted > 0;
        }

        public async Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(int days, int userId, DateTime startDate, bool useExternal)
        {
            if (days <= 0)
            {
                _logger.LogWarning("Days must be positive");
                throw new ArgumentException("Days must be positive.");
            }
            var mealPlan = await _recipeGenerator.GenerateMealPlan(days, userId, useExternal);
            mealPlan.StartDate = startDate;

            _logger.LogInformation("Meal plan successfully generated.");

            return mealPlan;
        }

        public async Task<GetPantryItemsResult> CookMeal(int id, int mealEntryId, int userId)
        {
            var mealPlan = _context.MealPlans.AsNoTracking().FirstOrDefault(m => m.Id == id);
            if (mealPlan == null)
            {
                _logger.LogWarning("Valid meal plan id required.");
                throw new ArgumentException("Valid meal plan id required.");
            }

            if (mealPlan.UserId != userId)
            {
                _logger.LogWarning("Given meal plan does not belong to current user.");
                throw new ValidationException("Given meal plan does not belong to current user.");
            }

            var mealPlanEntry = _context.MealPlanEntries
                .Include(m => m.Recipe)
                .ThenInclude(r => r.Ingredients)
                .FirstOrDefault(m => m.Id == mealEntryId);

            if (mealPlanEntry == null)
            {
                _logger.LogWarning("Valid meal plan entry id required.");
                throw new ArgumentException("Valid meal plan entry id required.");
            }

            if (mealPlanEntry.MealPlanId != id)
            {
                _logger.LogWarning("Given meal plan entry does not belong to the given meal plan.");
                throw new ValidationException("Given meal plan entry does not belong to the given meal plan.");
            }

            mealPlanEntry.Cooked = true;
            await _context.SaveChangesAsync();

            var pantry = _context.PantryItems.Where(p => p.UserId == userId).ToList();
            var recipeFoodIds = mealPlanEntry.Recipe.Ingredients.Select(i => i.FoodId).ToList();
            var usedPantryItems = pantry.Where(p => recipeFoodIds.Contains(p.FoodId)).ToList();

            return new GetPantryItemsResult { TotalCount = usedPantryItems.Count, Items = usedPantryItems.Select(p => p.ToDto()) }; 
        }

        private void VerifyRecipe(int? recipeId, int userId)
        {
            if (recipeId == null) return;
            if (_context.Recipes.FirstOrDefault(r => r.UserId == userId && r.Id == recipeId) != null) return;
            throw new ValidationException("Attempting to update meal plan with non-existent recipe.");
        }
    }
}
