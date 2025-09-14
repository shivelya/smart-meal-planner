using Backend.DTOs;
using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers
{
    public interface IRecipeGenerator
    {
        Task<CreateUpdateMealPlanRequestDto> GenerateMealPlan(int meals, int userId, bool useExternal);
    }

    public class ManualRecipeGenerator(PlannerContext context, ILogger<ManualRecipeGenerator> logger, IEnumerable<IExternalRecipeGenerator> generators) : IRecipeGenerator
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<ManualRecipeGenerator> _logger = logger;
        private readonly IEnumerable<IExternalRecipeGenerator> _generators = generators;

        public async Task<CreateUpdateMealPlanRequestDto> GenerateMealPlan(int mealCount, int userId, bool useExternal)
        {
            CreateUpdateMealPlanRequestDto mealPlanDto;
            if (!useExternal)
                mealPlanDto = await GenerateManually(mealCount, userId);
            else
                mealPlanDto = new CreateUpdateMealPlanRequestDto { Meals = [] };

            if (mealPlanDto.Meals.Count() == mealCount)
                return mealPlanDto;

            var pantry = _context.PantryItems.Include(p => p.Food).Where(p => p.UserId == userId);
            var meals = mealPlanDto.Meals.ToList();

            foreach (var generator in _generators)
            {
                if (meals.Count == mealCount) break;

                var mealPlanEntries = await generator.GenerateMealPlan(mealCount - meals.Count, pantry);
                meals.AddRange(mealPlanEntries);
            }

            mealPlanDto.Meals = meals;
            return mealPlanDto;
        }

        private async Task<CreateUpdateMealPlanRequestDto> GenerateManually(int meals, int userId)
        {
            // a recipe can only be chosen if the pantry has enough of all its ingredients
            // when a recipe is chosen, subtract its ingredient rquirements from the pantry
            // don't pick the same dominant ingredient repeatedly
            var recipes = await _context.Recipes
                .AsNoTracking()
                .Include(r => r.Ingredients)
                .ThenInclude(i => i.Food)
                .Where(r => r.UserId == userId)
                .ToListAsync();
            var pantry = await _context.PantryItems.AsNoTracking().Include(p => p.Food).Where(p => p.UserId == userId).ToListAsync();

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
                    break;

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
                    if (a.Quantity >= ing.Quantity)
                        // this ingredient is in the pantry
                        score += 2;
                    else
                        // this ingredient is in the pantry but not enough of it
                        score += 1;
                }
            }

            return score;
        }
    }
}