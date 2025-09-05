using System.Net;
using System.Reflection;
using System.Text.Json.Nodes;
using Backend.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Backend.Tests.Helpers
{
    public class RecipeExtractorTests
    {
        private readonly Mock<ILogger<RecipeExtractor>> _loggerMock = new();
        private readonly RecipeExtractor _extractor;
        private readonly Mock<HttpMessageHandler> handlerMock;

        public RecipeExtractorTests()
        {
            handlerMock = new Mock<HttpMessageHandler>();
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("your response here"),
                });

            var httpClient = new HttpClient(handlerMock.Object);
            _extractor = new RecipeExtractor(_loggerMock.Object, httpClient);
        }

        [Fact]
        public async Task ExtractRecipeAsync_ReturnsNull_WhenNoJsonLd()
        {
            // Setup a local server or mock HttpClient if needed, but for now, use a non-recipe page
            // This test will need to be adapted if HttpClient is injected
            // For now, just check that null is returned for a bad URL
            var result = await _extractor.ExtractRecipeAsync("https://example.com/no-recipe");
            Assert.Null(result);

        }

        [Fact]
        public async Task ExtractRecipeAsync_ReturnsRecipe_WhenValidJsonLdRecipe()
        {
            var jsonLd = "<script type='application/ld+json'>{\"@type\":\"Recipe\",\"name\":\"Test Recipe\",\"recipeIngredient\":[\"1 cup sugar\"],\"recipeInstructions\":[\"Step 1\"]}</script>";
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"<html>{jsonLd}</html>")
                });
            var result = await _extractor.ExtractRecipeAsync("https://example.com/recipe");
            Assert.NotNull(result);
            Assert.Equal("Test Recipe", result.Title);
            Assert.Single(result.Ingredients);
            Assert.Equal("1", result.Ingredients[0].Quantity);
            Assert.Equal("cup", result.Ingredients[0].Unit);
            Assert.Equal("sugar", result.Ingredients[0].Name);
            Assert.Equal("Step 1", result.Instructions);
        }

        [Fact]
        public async Task ExtractRecipeAsync_IgnoresParseErrorsAndContinues()
        {
            var badJsonLd = "<script type='application/ld+json'>{not valid json}</script>";
            var goodJsonLd = "<script type='application/ld+json'>{\"@type\":\"Recipe\",\"name\":\"Good\",\"recipeIngredient\":[\"2 tbsp flour\"],\"recipeInstructions\":[\"Mix\"]}</script>";
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"<html>{badJsonLd}{goodJsonLd}</html>")
                });
            var result = await _extractor.ExtractRecipeAsync("https://example.com/recipe2");
            Assert.NotNull(result);
            Assert.Equal("Good", result.Title);
            Assert.Single(result.Ingredients);
            Assert.Equal("2", result.Ingredients[0].Quantity);
            Assert.Equal("tbsp", result.Ingredients[0].Unit);
            Assert.Equal("flour", result.Ingredients[0].Name);
            Assert.Equal("Mix", result.Instructions);
        }

        [Fact]
        public async Task ExtractRecipeAsync_HandlesArrayWrappedRecipeObject()
        {
            var arrayJsonLd = "<script type='application/ld+json'>[{\"@type\":\"Recipe\",\"name\":\"Array Recipe\",\"recipeIngredient\":[\"3 tsp salt\"],\"recipeInstructions\":[\"Add\"]}]</script>";
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent($"<html>{arrayJsonLd}</html>")
                });
            var result = await _extractor.ExtractRecipeAsync("https://example.com/array-recipe");
            Assert.NotNull(result);
            Assert.Equal("Array Recipe", result.Title);
            Assert.Single(result.Ingredients);
            Assert.Equal("3", result.Ingredients[0].Quantity);
            Assert.Equal("tsp", result.Ingredients[0].Unit);
            Assert.Equal("salt", result.Ingredients[0].Name);
            Assert.Equal("Add", result.Instructions);
        }

        [Fact]
        public void ParseIngredient_ReturnsEmptyTuple_WhenInputIsEmpty()
        {
            var tuple = _extractor.GetType().GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("")]);
            Assert.Null(tuple);
        }

        [Fact]
        public void ParseIngredient_ParsesSimpleIngredient()
        {
            var actual = _extractor.GetType().GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("1 cup sugar")]);
            var expected = new ExtractedIngredient { Quantity = "1", Unit = "cup", Name = "sugar" };
            var ingredient = Assert.IsType<ExtractedIngredient>(actual);
            Assert.Equal(expected.Quantity, ingredient.Quantity);
            Assert.Equal(expected.Unit, ingredient.Unit);
            Assert.Equal(expected.Name, ingredient.Name);
        }

        [Fact]
        public void ParseIngredient_FallbacksToUnit_WhenNoMatch()
        {
            var actual = _extractor.GetType().GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("justsomestring")]);
            var expected = new ExtractedIngredient { Quantity = "", Unit = "", Name = "justsomestring" };
            var ingredient = Assert.IsType<ExtractedIngredient>(actual);
            Assert.Equal(expected.Quantity, ingredient.Quantity);
            Assert.Equal(expected.Unit, ingredient.Unit);
            Assert.Equal(expected.Name, ingredient.Name);
        }

        [Fact]
        public void ExtractInstructions_HandlesStringNode()
        {
            var node = JsonValue.Create("Step 1. Do something.");
            var result = _extractor.GetType().GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [node]);
            Assert.Equal("Step 1. Do something.", result);
        }

        [Fact]
        public void ExtractInstructions_HandlesArrayNode()
        {
            var arr = new JsonArray { JsonValue.Create("Step 1"), JsonValue.Create("Step 2") };
            var result = _extractor.GetType().GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [arr]);
            Assert.Equal("Step 1\nStep 2", result);
        }

        [Fact]
        public void ExtractInstructions_HandlesArrayOfObjects()
        {
            var arr = new JsonArray { new JsonObject { ["text"] = "Step 1" }, new JsonObject { ["text"] = "Step 2" } };
            var result = _extractor.GetType().GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [arr]);
            Assert.Equal("Step 1\nStep 2", result);
        }

        [Fact]
        public async Task ExtractRecipeAsync_ReturnsNull_OnParseError()
        {
            // Simulate HTML with invalid JSON-LD
            var html = "<script type='application/ld+json'>{ invalid json }</script>";
            handlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(html),
                });

            // AngleSharp mock
            // You may need to mock BrowsingContext and document.QuerySelectorAll to return the script content

            // This test will hit the catch branch and return null
            var result = await _extractor.ExtractRecipeAsync("http://test");
            Assert.Null(result);
        }

        [Fact]
        public void ParseIngredient_ReturnsNull_OnEmptyString()
        {
            var result = _extractor.GetType()
                .GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("")]);

            Assert.Null(result);
        }

        [Fact]
        public void ParseIngredient_RegexMatch_ReturnsParsed()
        {
            var result = _extractor.GetType()
                .GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("1 cup sugar")]) as ExtractedIngredient;

            Assert.Equal("1", result!.Quantity);
            Assert.Equal("cup", result.Unit);
            Assert.Equal("sugar", result.Name);
        }

        [Fact]
        public void ParseIngredient_Fallback_ReturnsName()
        {
            var result = _extractor.GetType()
                .GetMethod("ParseIngredient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("justsomename")]) as ExtractedIngredient;

            Assert.Equal("justsomename", result!.Name);
        }

        [Fact]
        public void ExtractInstructions_ReturnsNull_WhenNodeIsNull()
        {
            var result = _extractor.GetType()
                .GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [null]);

            Assert.Null(result);
        }

        [Fact]
        public void ExtractInstructions_ArrayOfObjects_ReturnsJoinedText()
        {
            var arr = new JsonArray
            {
                new JsonObject { ["text"] = "Step 1" },
                new JsonObject { ["text"] = "Step 2" }
            };

            var result = _extractor.GetType()
                .GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [arr]) as string;

            Assert.Equal("Step 1\nStep 2", result);
        }

        [Fact]
        public void ExtractInstructions_ArrayOfStrings_ReturnsJoinedText()
        {
            var arr = new JsonArray { "Step 1", "Step 2" };

            var result = _extractor.GetType()
                .GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [arr]) as string;

            Assert.Equal("Step 1\nStep 2", result);
        }

        [Fact]
        public void ExtractInstructions_SingleString_ReturnsText()
        {
            var result = _extractor.GetType()
                .GetMethod("ExtractInstructions", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(_extractor, [JsonValue.Create("Step 1")]) as string;

            Assert.Equal("Step 1", result);
        }
    }
}
