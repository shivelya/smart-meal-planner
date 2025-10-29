using System.Net;
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
        Task<ExtractedRecipe?> ExtractRecipeAsync(string url, CancellationToken ct = default);
        Task<ExtractedRecipe> ExtractRecipeByTextAsync(string source, CancellationToken ct = default);
    }

    public partial class ManualRecipeExtractor(ILogger<ManualRecipeExtractor> logger, HttpClient httpClient) : IRecipeExtractor
    {
        [GeneratedRegex(@"^(?<qty>(\d+([.,]\d+)?|\d+\s*/\s*\d+|[¼½¾⅓⅔⅛⅜⅝⅞]))?\s*
                (?:(?<unit>[a-zA-Z]+)\s+(?<name>.*) | (?<name>.+))$", RegexOptions.IgnorePatternWhitespace)]
        private static partial Regex IngredientRegex();

        private readonly ILogger<ManualRecipeExtractor> _logger = logger;
        private readonly HttpClient _httpClient = httpClient;

        public Task<ExtractedRecipe> ExtractRecipeByTextAsync(string source, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ExtractedRecipe?> ExtractRecipeAsync(string url, CancellationToken ct = default)
        {
            const string method = nameof(ExtractRecipeAsync);
            _logger.LogInformation("{Method}: Entering method with {Url}", method, url);

            // 1. Download the HTML
            string html = await DownloadHtml(url, ct);

            // 2. Parse with AngleSharp
            var context = BrowsingContext.New(Configuration.Default);
            var document = await context.OpenAsync(req => req.Content(html), ct);

            // 3. Find JSON-LD blocks
            return ParseJsonBlocks(url, document);
        }

        private ExtractedRecipe? ParseJsonBlocks(string url, IDocument document)
        {
            const string method = nameof(ParseJsonBlocks);
            _logger.LogInformation("{Method}: Entering method", method);

            var scriptElements = document
                            .QuerySelectorAll("script[type='application/ld+json']")
                            .Select(el => el.TextContent);

            foreach (var json in scriptElements)
            {
                _logger.LogDebug("{Method}: Found JSON-LD block: {JsonPreview}", method, json.Length > 200 ? json[..200] + "..." : json);
                try
                {
                    var node = JsonNode.Parse(json);
                    if (node == null) continue;

                    // Sometimes it's wrapped in an array. If not then wrap it so we can treat uniformly
                    var candidates = node is JsonArray arr ? arr : new JsonArray(node);

                    foreach (var item in candidates)
                    {
                        var type = item?["@type"]?.ToString();
                        if (type == null) continue;

                        if (type.Contains("Recipe"))
                        {
                            var recipe = new ExtractedRecipe
                            {
                                Title = item?["name"]?.ToString() ?? string.Empty,
                                Instructions = ExtractInstructions(item?["recipeInstructions"])
                            };

                            _logger.LogDebug("{Method}: Extracted recipe title: {Title}", method, recipe.Title);

                            var ingredients = item?["recipeIngredient"]? // find recipeIngredient nodes
                                .AsArray() // get as array
                                .Select(ParseIngredient) // parse each ingredient into an ExtractedIngredient? object
                                .Where(s => s != null) // filter out nulls for where parsing failed
                                .Cast<ExtractedIngredient>(); // cast to non-nullable type now that nulls have been removed

                            if (ingredients != null && ingredients.Any())
                                recipe.Ingredients.AddRange(ingredients);

                            _logger.LogInformation("{Method}: Successfully exiting method", method);
                            return recipe;
                        }
                    }
                }
                catch
                {
                    _logger.LogInformation("{Method}: Found parse errors extracting recipe, ignoring them.", method);
                    // Ignore parse errors, try next block
                }
            }

            _logger.LogInformation("{Method}: No recipe found in the provided URL: {Url}", method, url);
            _logger.LogInformation("{Method}: Exiting method", method);
            return null;
        }

        private async Task<string> DownloadHtml(string url, CancellationToken ct)
        {
            const string method = nameof(DownloadHtml);
            _logger.LogInformation("{Method}: Entering with URL: {Url}", method, url);

            string? html;
            try
            {
                html = await _httpClient.GetStringAsync(url, ct);
                _logger.LogInformation("{Method}: Successfully downloaded HTML content from {Url}", method, url);
                _logger.LogDebug("{Method}: HTML content length: {Length}", method, html.Length);
                _logger.LogDebug("{Method}: HTML content preview: {Preview}", method, html[..Math.Min(500, html.Length)]);
            }
            catch (UriFormatException ex)
            {
                // this is checked in the controller already
                _logger.LogWarning("{Method}: Invalid URL format provided: {Url}", method, url);
                throw new ArgumentException("The provided URL is not valid.", ex);
            }
            catch (NotSupportedException ex)
            {
                // unsupported schema
                _logger.LogWarning(ex, "{Method}: The URL has an unsupported schema: {Url}", method, url);
                throw new ArgumentException("The provided URL has an unsupported format.", ex);
            }
            catch (HttpRequestException ex)
            {
                // URL is well formatted but request failed
                _logger.LogWarning(ex, "{Method}: Error downloading recipe from URL: {Url}", method, url);
                throw new HttpRequestException("Error downloading recipe", ex, HttpStatusCode.BadGateway);
            }

            _logger.LogInformation("{Method}: Exiting with downloaded HTML content", method);
            return html;
        }

        private ExtractedIngredient? ParseIngredient(JsonNode? node)
        {
            const string method = nameof(ParseIngredient);
            var ingredient = node?.ToString() ?? "";

            _logger.LogInformation("{Method}: Entering with node: {Node}", method, ingredient);
            if (string.IsNullOrWhiteSpace(ingredient))
            {
                _logger.LogInformation("{Method}: Skipping ingredient, it's empty", method);
                _logger.LogInformation("{Method}: Exiting with null", method);
                return null;
            }

            // Example regex: matches "1 cup sugar" -> "1" | "cup sugar"
            // Supports fractions like "1/2" or "½" and decimals
            var regex = IngredientRegex();

            var match = regex.Match(ingredient.Trim());
            if (match.Success)
            {
                var quantity = match.Groups["qty"].Value.Trim();
                var unit = match.Groups["unit"].Value.Trim();
                var name = match.Groups["name"].Value.Trim();
                _logger.LogDebug("{Method}: Parsed ingredient - Quantity: {Quantity}, Unit: {Unit}, Name: {Name}",
                    method, quantity, unit, name);

                _logger.LogInformation("{Method}: Exiting with parsed ingredient", method);
                return new ExtractedIngredient
                {
                    Quantity = quantity,
                    Unit = unit,
                    Name = name
                };
            }

            // Fallback: stick everything in "Name"
            _logger.LogInformation("{Method}: Could not parse ingredient structure, defaulting to Name only", method);
            _logger.LogInformation("{Method}: Exiting with parsed ingredient", method);
            return new ExtractedIngredient { Name = ingredient.Trim() };
        }

        private string? ExtractInstructions(JsonNode? instructionsNode)
        {
            const string method = nameof(ExtractInstructions);
            _logger.LogInformation("{Method}: Entering with node: {Node}", method, instructionsNode);

            if (instructionsNode == null)
            {
                _logger.LogInformation("{Method}: No instructions found while extracting recipe.", method);
                _logger.LogInformation("{Method}: Exiting with null", method);
                return null;
            }

            var instructions = new List<string>();

            // Can be a string, array of strings, or array of objects
            if (instructionsNode is JsonArray arr)
            {
                foreach (var step in arr)
                {
                    if (step is JsonObject obj && obj.ContainsKey("text"))
                        // array of objects
                        instructions.Add(obj["text"]?.ToString() ?? "");
                    else
                        // array of strings
                        instructions.Add(step?.ToString() ?? "");
                }
            }
            else
            {
                // string
                instructions.Add(instructionsNode.ToString());
            }

            var joinedInstructions = string.Join("\n", instructions.Where(s => !string.IsNullOrWhiteSpace(s)).ToList());
            _logger.LogDebug("{Method}: Extracted instructions: {Instructions}", method, joinedInstructions);
            _logger.LogInformation("{Method}: Exiting with instructions", method);

            return joinedInstructions;
        }
    }
}