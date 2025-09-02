using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace Backend.Helpers
{
    public class ExtractedRecipe
    {
        public required string Title { get; set; }
        public List<ExtractedIngredient> Ingredients { get; set; } = [];
        public string? Instructions { get; set; }
    }

    public class ExtractedIngredient
    {
        public string? Quantity { get; set; }
        public string? Unit { get; set; }
        public required string Name { get; set; }
    }

    public interface IRecipeExtractor
    {
        Task<ExtractedRecipe?> ExtractRecipeAsync(string url);
    }

    public class RecipeExtractor(ILogger<RecipeExtractor> logger, HttpClient httpClient) : IRecipeExtractor
    {
        private readonly ILogger<RecipeExtractor> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;

        public async Task<ExtractedRecipe?> ExtractRecipeAsync(string url)
        {
            // 1. Download the HTML
            var html = await _httpClient.GetStringAsync(url);

            // 2. Parse with AngleSharp
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html));

            // 3. Find JSON-LD blocks
            var scriptElements = document
                .QuerySelectorAll("script[type='application/ld+json']")
                .Select(el => el.TextContent);

            foreach (var json in scriptElements)
            {
                try
                {
                    var node = JsonNode.Parse(json);
                    if (node == null) continue;

                    // Sometimes it's wrapped in an array
                    var candidates = node is JsonArray arr ? arr : new JsonArray(node);

                    foreach (var item in candidates)
                    {
                        var type = item?["@type"]?.ToString();
                        if (string.Equals(type, "Recipe", StringComparison.OrdinalIgnoreCase))
                        {
                            var recipe = new ExtractedRecipe
                            {
                                Title = item?["name"]?.ToString() ?? string.Empty,
                                Instructions = ExtractInstructions(item?["recipeInstructions"])
                            };
                            recipe.Ingredients.AddRange(item?["recipeIngredient"]?.AsArray().Select(ParseIngredient)
                                .Where(s => s != null && (!string.IsNullOrWhiteSpace(s.Quantity) || !string.IsNullOrWhiteSpace(s.Unit)))
                                .Cast<ExtractedIngredient>().ToList() ?? []);

                            return recipe;
                        }
                    }
                }
                catch
                {
                    _logger.LogInformation("Found parse errors extracting recipe, ignoring them.");
                    // Ignore parse errors, try next block
                }
            }

            return null;
        }

        private ExtractedIngredient? ParseIngredient(JsonNode? node)
        {
            var ingredient = node?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(ingredient))
            {
                _logger.LogInformation("Skipping ingredient, it's empty");
                return null;
            }

            // Example regex: matches "1 cup sugar" -> "1" | "cup sugar"
            // Supports fractions like "1/2" or "½" and decimals
            var regex = new Regex(
                @"^(?<qty>(\d+([.,]\d+)?|\d+\s*/\s*\d+|[¼½¾⅓⅔⅛⅜⅝⅞]))?\s*
                (?:(?<unit>[a-zA-Z]+)\s+(?<name>.*) | (?<name>.+))$",
                RegexOptions.IgnorePatternWhitespace
            );

            var match = regex.Match(ingredient.Trim());
            if (match.Success)
            {
                return new ExtractedIngredient
                {
                    Quantity = match.Groups["qty"].Value.Trim(),
                    Unit = match.Groups["unit"].Value.Trim(),
                    Name = match.Groups["name"].Value.Trim()
                };
            }

            // Fallback: stick everything in "Name"
            return new ExtractedIngredient { Name = ingredient.Trim() };
        }

        private string? ExtractInstructions(JsonNode? instructionsNode)
        {
            if (instructionsNode == null)
            {
                _logger.LogInformation("No instructions found while extracting recipe.");
                return null;
            }

            var instructions = new List<string>();

            // Can be a string, array of strings, or array of objects
            if (instructionsNode is JsonArray arr)
            {
                foreach (var step in arr)
                {
                    if (step is JsonObject obj && obj.ContainsKey("text"))
                        instructions.Add(obj["text"]?.ToString() ?? "");
                    else
                        instructions.Add(step?.ToString() ?? "");
                }
            }
            else
            {
                instructions.Add(instructionsNode.ToString());
            }

            return string.Join("\n", instructions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList());
        }
    }
}