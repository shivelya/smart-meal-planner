using Backend.DTOs;
using Backend.Helpers;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class RecipeGeneratorService(PlannerContext context, ILogger<RecipeGeneratorService> logger, IEnumerable<IExternalRecipeGenerator> generators) : IRecipeGenerator
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<RecipeGeneratorService> _logger = logger;
        private readonly IEnumerable<IExternalRecipeGenerator> _generators = generators;

        public async Task<CreateUpdateMealPlanRequestDto> GenerateMealPlanAsync(int mealCount, int userId, bool useExternal)
        {
            _logger.LogInformation("Entering GenerateMealPlanAsync: userId={UserId}, mealCount={MealCount}, useExternal={UseExternal}", userId, mealCount, useExternal);
            if (mealCount <= 0)
            {
                _logger.LogWarning("GenerateMealPlanAsync: Called with non-positive mealCount: {MealCount}", mealCount);
                throw new ArgumentException("Meal count must be greater than zero.", nameof(mealCount));
            }

            if (!_context.Users.Any(u => u.Id == userId))
            {
                _logger.LogWarning("GenerateMealPlanAsync: Called with non-existent userId: {UserId}", userId);
                throw new ArgumentException("User does not exist.", nameof(userId));
            }

            // we have two modes of operation:
            // if not using external, generate meal plan manually. Then if we don't have enough meals, fall back to external generators
            // if using external, call each generator in turn until we have enough meals
            CreateUpdateMealPlanRequestDto mealPlanDto;
            if (!useExternal)
                mealPlanDto = await GenerateManually(mealCount, userId);
            else
                mealPlanDto = new CreateUpdateMealPlanRequestDto { Meals = [] };

            if (mealPlanDto.Meals.Count() == mealCount)
            {
                _logger.LogInformation("Generated meal plan with {MealCount} meals for user {UserId} using manual generation.", mealCount, userId);
                _logger.LogInformation("Exiting GenerateMealPlanAsync: userId={UserId}, totalMeals={TotalMeals}", userId, mealPlanDto.Meals.Count());
                return mealPlanDto;
            }

            // get pantry items
            // a recipe can only be chosen if the pantry has enough of all its ingredients
            var pantry = await _context.PantryItems
                .AsNoTracking()
                .Include(p => p.Food)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            // copy to a list so we can add to it
            var meals = mealPlanDto.Meals.ToList();

            foreach (var generator in _generators)
            {
                // if we have enough meals, break out
                if (meals.Count == mealCount) break;

                // call the generator
                _logger.LogInformation("Calling external recipe generator {GeneratorName} for user {UserId}.", generator.GetType().Name, userId);
                var mealPlanEntries = await generator.GenerateMealPlanAsync(mealCount - meals.Count, pantry);
                meals.AddRange(mealPlanEntries);
            }

            mealPlanDto.Meals = meals;
            _logger.LogInformation("Exiting GenerateMealPlanAsync: userId={UserId}, totalMeals={TotalMeals}", userId, meals.Count);
            return mealPlanDto;
        }

        private async Task<CreateUpdateMealPlanRequestDto> GenerateManually(int meals, int userId)
        {
            _logger.LogInformation("Entering GenerateManually: userId={UserId}, meals={Meals}", userId, meals);
            // a recipe can only be chosen if the pantry has enough of all its ingredients
            // when a recipe is chosen, subtract its ingredient rquirements from the pantry
            // don't pick the same dominant ingredient repeatedly

            var pantry = await _context.PantryItems
                .AsNoTracking()
                .Include(p => p.Food)
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var selectedRecipes = new List<Recipe>();

            // select recipes
            for (int i = 0; i < meals; i++)
            {
                // score all the recipes
                // key: recipe id, value: score
                var score = new Dictionary<int, int>();
                foreach (var r in recipes)
                    score.Add(r.Id, ScoreRecipe(r, pantry));

                // remove recipes with zero score so they aren't used next go round
                // they don't contain any ingredients
                foreach (var a in score)
                    if (a.Value <= 0)
                        recipes.Remove(recipes.First(r => r.Id == a.Key));

                // we ran out of recipes that have ingredients, so cut out
                if (recipes.Count == 0)
                {
                    _logger.LogWarning("GenerateManually: No more recipes available for userId={UserId}", userId);
                    break;
                }

                // choose the highest scored recipe
                var highestScore = score.MaxBy(e => e.Value);
                var selectedRecipe = recipes.First(r => r.Id == highestScore.Key);

                // remove it from recipe list
                recipes.Remove(selectedRecipe);

                // remove any ingredients in the pantry from the pantry list
                foreach (var ing in selectedRecipe.Ingredients)
                {
                    // see if there's a matching ingredient
                    var pantryItem = pantry.FirstOrDefault(p => p.FoodId == ing.FoodId);
                    if (pantryItem == null) continue;

                    // remove it. For now, we don't have the sophistication to compare units in order to subtract quantities
                    pantry.Remove(pantryItem);
                }

                // add the recipe to our meal plan
                selectedRecipes.Add(selectedRecipe);
            }

            var generatedMealPlan = new CreateUpdateMealPlanRequestDto
            {
                Meals = selectedRecipes.Select(r => new CreateUpdateMealPlanEntryRequestDto { RecipeId = r.Id })
            };

            _logger.LogInformation("Exiting GenerateManually: userId={UserId}, selectedRecipes={SelectedRecipes}", userId, selectedRecipes.Count);
            return generatedMealPlan;
        }

        private static int ScoreRecipe(Recipe recipe, List<PantryItem> pantry)
        {
            int score = 0;
            // favor recipes that use up items that exist in pantry
            // create a coverage score: % of ingredients in pantry vs how many need shopping

            // loop through each ingredient
            foreach (var ing in recipe.Ingredients)
            {
                // recipes only get points when they contain ingredients
                var a = pantry.FirstOrDefault(p => p.Food.Name.Equals(ing.Food.Name, StringComparison.CurrentCultureIgnoreCase));
                if (a != null)
                {
                    // we have this ingredient in the pantry
                    // we aren't sophisticated enough to compare units, so it gets a flat score
                    score += 2;

                    // TODO: we could do this later
                    // if (a.Quantity >= ing.Quantity)
                    //     // this ingredient is in the pantry
                    //     score += 2;
                    // else
                    //     // this ingredient is in the pantry but not enough of it
                    //     score += 1;
                }
            }

            return score;
        }
    }
}