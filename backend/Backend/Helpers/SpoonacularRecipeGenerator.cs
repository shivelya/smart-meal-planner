using System.Globalization;
using Backend.DTOs;
using Backend.Model;
using Microsoft.AspNetCore.WebUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Backend.Helpers
{
    public interface IExternalRecipeGenerator
    {
        Task<IEnumerable<GeneratedMealPlanEntryDto>> GenerateMealPlanAsync(int meals, IEnumerable<PantryItem> pantry, CancellationToken ct);
    }

    public class SpoonacularRecipeGenerator(ILogger<SpoonacularRecipeGenerator> logger, HttpClient httpClient, IConfiguration configuration) : IExternalRecipeGenerator
    {
        private readonly ILogger<SpoonacularRecipeGenerator> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;
        private readonly IConfiguration _configuration = configuration;

        //expects the PantryItem objects to include the Food object
        public async Task<IEnumerable<GeneratedMealPlanEntryDto>> GenerateMealPlanAsync(int meals, IEnumerable<PantryItem> pantry, CancellationToken ct)
        {
            // GET https://api.spoonacular.com/recipes/findByIngredients
            //ingredients=apples,+bananas
            //number=10 - maximum number of recipes to return
            //ranking=1 - whether to maximize used ingredients (1) or minimize missing ingredients (2)
            //ignorePantry=true - whether to ignore typical pantry items, such as water, salt, flour, etc.

            var ingredientsStr = string.Join(",", pantry.Select(p => p.Food.Name));
            var ingredients = Uri.EscapeDataString(ingredientsStr);
            _httpClient.DefaultRequestHeaders.Add("x-api-key", _configuration["Spoonacular:ApiKey"]);
            var url = QueryHelpers.AddQueryString(
                "https://api.spoonacular.com/recipes/complexSearch",
                new Dictionary<string, string?>
                {
                    { "includeIngredients", ingredients }, // comma-delimited ingredients that must be included
                    { "number", meals.ToString(CultureInfo.InvariantCulture) }, // the number to return
                    { "sort", "max-used-ingredients" }, // sort by the best options so we get the best options
                    { "addRecipeInstructions", true.ToString() }, // we want instructions in our generated object
                    { "type", "main course" } // so we don't get things like side dishes or breakfasts
                }
            );

            var genList = new List<GeneratedMealPlanEntryDto>();
            try
            {
                var response = await _httpClient.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync(ct);
                var obj = JObject.Parse(json);
                var recipes = (JArray)obj["results"]!;

                // returns an array of recipes
                foreach (var recipe in recipes)
                {
                    // need to make separate call to get instructions as text, womp womp
                    response = await _httpClient.GetAsync($"https://api.spoonacular.com/recipes/{recipe["id"]}/information", ct);
                    response.EnsureSuccessStatusCode();
                    json = await response.Content.ReadAsStringAsync(ct);
                    obj = JObject.Parse(json);

                    var gen = new GeneratedMealPlanEntryDto
                    {
                        Title = (string)recipe["title"]!,
                        Source = $"Spoonacular - {((int)recipe["id"]!).ToString(CultureInfo.InvariantCulture)}",
                        Instructions = (string)obj["instructions"]!
                    };

                    genList.Add(gen);
                }

                return genList;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "GeneratedMealPlanAsync: Unable to successfully reach Spoonacular.");
                return [];
            }
            catch (JsonReaderException ex)
            {
                _logger.LogWarning(ex, "GeneratedMealPlanAsync: Invalid JSON returned from Spoonacular.");
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GeneratedMealPlanAsync: Exception thrown");
                return [];
            }


            // for getting food images and category information

            //https://api.spoonacular.com/food/ingredients/search?query=apple&metInformation=true&apiKey=052d8dc538d747fcb8fb7a49ee18f3d8
            // gets an ingredient and finds its id number

            //https://api.spoonacular.com/food/ingredients/9003/information?apiKey=052d8dc538d747fcb8fb7a49ee18f3d8
            // users the ingredients id to get an image url and an Aisle, or Category

            //https://img.spoonacular.com/ingredients_100x100/{ingredient-image-path}
            //gets an image for an ingredient

            // for getting recipes images

            //https://img.spoonacular.com/recipes/{ID}-{SIZE}.jpg
            // gets an image for a recipe
            // ID is the recipe id
            // size is one of - 90x90, 240x150, 312x150, 312x231, 480x360, 556x370, or 636x393
        }
    }
}