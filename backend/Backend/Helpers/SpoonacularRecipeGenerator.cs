using Backend.DTOs;
using Backend.Model;

namespace Backend.Helpers
{
    public interface IExternalRecipeGenerator
    {
        IEnumerable<GeneratedMealPlanEntryDto> GenerateMealPlan(int meals, IEnumerable<PantryItem> pantry);
    }

    public class SpoonacularRecipeGenerator(ILogger<SpoonacularRecipeGenerator> logger, HttpClient httpClient) : IExternalRecipeGenerator
    {
        private readonly ILogger<SpoonacularRecipeGenerator> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;

        //expects the PantryItem objects to include the Food object
        public IEnumerable<GeneratedMealPlanEntryDto> GenerateMealPlan(int meals, IEnumerable<PantryItem> pantry)
        {
            // GET https://api.spoonacular.com/recipes/findByIngredients
            //ingredients=apples,+bananas
            //number=10 - maximum number of recipes to return
            //ranking=1 - whether to maximize used ingredients (1) or minimize missing ingredients (2)
            //ignorePantry=true - whether to ignore typical pantry items, such as water, salt, flour, etc.

            //https://api.spoonacular.com/food/ingredients/search?query=apple&metInformation=true&apiKey=052d8dc538d747fcb8fb7a49ee18f3d8
            // gets an ingredient and finds its id number

            //https://api.spoonacular.com/food/ingredients/9003/information?apiKey=052d8dc538d747fcb8fb7a49ee18f3d8
            // users the ingredients id to get an image url and an Aisle, or Category

            //https://img.spoonacular.com/ingredients_100x100/{ingredient-image-path}
            //gets an image for an ingredient

            //https://img.spoonacular.com/recipes/{ID}-{SIZE}.jpg
            // gets an image for a recipe
            // ID is the recipe id
            // size is one of - 90x90, 240x150, 312x150, 312x231, 480x360, 556x370, or 636x393
            throw new NotImplementedException();
        }
    }
}