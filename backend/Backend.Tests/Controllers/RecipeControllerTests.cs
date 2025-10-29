using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Controllers
{
    public class RecipeControllerTests
    {
        private readonly Mock<IRecipeService> _serviceMock = new();
        private readonly Mock<ILogger<RecipeController>> _loggerMock = new();
        private readonly Mock<IRecipeExtractor> _extractorMock = new();
        private readonly RecipeController _controller;

        public RecipeControllerTests()
        {
            _controller = new RecipeController(_serviceMock.Object, _loggerMock.Object, _extractorMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "1")]));
            _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        }

        [Fact]
        public async Task Create_ReturnsCreated_WhenSuccess()
        {
            var dto = new Mock<CreateUpdateRecipeDtoRequest>().Object;
            var resultDto = new RecipeDto { Id = 1 };
            _serviceMock.Setup(s => s.CreateAsync(dto, 1, CancellationToken.None)).ReturnsAsync(resultDto);
            var result = await _controller.CreateAsync(dto, CancellationToken.None);
            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(resultDto, created.Value);
        }
            [Fact]
            public async Task Create_Returns500_WhenServiceReturnsNull()
            {
                var dto = new Mock<CreateUpdateRecipeDtoRequest>().Object;
                _serviceMock.Setup(s => s.CreateAsync(dto, 1, CancellationToken.None)).ReturnsAsync((RecipeDto)null!);
                var result = await _controller.CreateAsync(dto, CancellationToken.None);
                var status = Assert.IsType<StatusCodeResult>(result.Result);
                Assert.Equal(500, status.StatusCode);
            }

        [Fact]
        public async Task Create_Returns500_OnException()
        {
            var dto = new Mock<CreateUpdateRecipeDtoRequest>().Object;
            _serviceMock.Setup(s => s.CreateAsync(dto, 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.CreateAsync(dto, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var recipe = new RecipeDto { Id = 2 };
            _serviceMock.Setup(s => s.GetByIdAsync(2, 1, CancellationToken.None)).ReturnsAsync(recipe);
            var result = await _controller.GetByIdAsync(2, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(recipe, ok.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(3, 1, CancellationToken.None)).ReturnsAsync((RecipeDto?)null);
            var result = await _controller.GetByIdAsync(3, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(4, 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetByIdAsync(4, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsOk_WhenSuccess()
        {
            var ids = new[] { 1, 2 };
            var recipesRequest = new GetRecipesRequest { Ids = ids };
            var recipes = new List<RecipeDto> { new() { Id = 1 }, new() { Id = 2 } };
            var recipesResult = new GetRecipesResult { TotalCount = 2, Items = recipes };
            _serviceMock.Setup(s => s.GetByIdsAsync(ids, 1, CancellationToken.None)).ReturnsAsync(recipesResult);
            var result = await _controller.GetByIdsAsync(recipesRequest, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var r = Assert.IsType<GetRecipesResult>(ok.Value);
            Assert.Equal(recipes, r.Items);
        }

        [Fact]
        public async Task GetByIds_Returns500_OnException()
        {
            var ids = new[] { 1, 2 };
            var recipesRequest = new GetRecipesRequest { Ids = ids };
            _serviceMock.Setup(s => s.GetByIdsAsync(ids, 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetByIdsAsync(recipesRequest, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetByIds_Returns500_WhenServiceReturnsNull()
        {
            var ids = new[] { 1, 2 };
            var recipesRequest = new GetRecipesRequest { Ids = ids };
            _serviceMock.Setup(s => s.GetByIdsAsync(ids, 1, CancellationToken.None)).ReturnsAsync((GetRecipesResult)null!);
            var result = await _controller.GetByIdsAsync(recipesRequest, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.GetByIdsAsync(null!, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsBadRequest_WhenIdsIsNull()
        {
            var result = await _controller.GetByIdsAsync(new GetRecipesRequest { Ids = null! }, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsOk_WhenSuccess()
        {
            var recipes = new List<RecipeDto> { new RecipeDto { Id = 1 } };
            var recipesResult = new GetRecipesResult { TotalCount = 1, Items = recipes };
            _serviceMock.Setup(s => s.SearchAsync(It.IsAny<int>(), "Pizza", null, 0, 10, CancellationToken.None)).ReturnsAsync(recipesResult);
            var result = await _controller.SearchAsync("Pizza", null, 0, 10, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var r = Assert.IsType<GetRecipesResult>(ok.Value);
            Assert.Equal(recipes, r.Items);
        }

        [Fact]
        public async Task Search_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.SearchAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.SearchAsync("Pizza", null, 0, 10, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }
            [Fact]
            public async Task Search_Returns500_WhenServiceReturnsNull()
            {
                _serviceMock.Setup(s => s.SearchAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(), CancellationToken.None)).ReturnsAsync((GetRecipesResult)null!);
                var result = await _controller.SearchAsync("Pizza", null, 0, 10, CancellationToken.None);
                var status = Assert.IsType<StatusCodeResult>(result.Result);
                Assert.Equal(500, status.StatusCode);
            }

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            var updated = new RecipeDto { Id = 1 };
            _serviceMock.Setup(s => s.UpdateAsync(1, dto, 1, CancellationToken.None)).ReturnsAsync(updated);
            var result = await _controller.UpdateAsync(1, dto, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(updated, ok.Value);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            _serviceMock.Setup(s => s.UpdateAsync(2, dto, 1, CancellationToken.None)).ReturnsAsync((RecipeDto)null!);
            var result = await _controller.UpdateAsync(2, dto, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Update_Returns500_OnException()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            _serviceMock.Setup(s => s.UpdateAsync(3, dto, 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.UpdateAsync(3, dto, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenSuccess()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1, 1, CancellationToken.None)).ReturnsAsync(true);
            var result = await _controller.DeleteAsync(1, CancellationToken.None);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenFalse()
        {
            _serviceMock.Setup(s => s.DeleteAsync(2, 1, CancellationToken.None)).ReturnsAsync(false);
            var result = await _controller.DeleteAsync(2, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.DeleteAsync(3, 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.DeleteAsync(3, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsOk_OnSuccess()
        {
            var extracted = new ExtractedRecipe { Title = "test" };
            var request = new ExtractRequest { Source = "https://test.com" };
            _extractorMock.Setup(e => e.ExtractRecipeAsync(request.Source, CancellationToken.None)).ReturnsAsync(extracted);
            var result = await _controller.ExtractRecipeAsync(request, CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(extracted, ok.Value);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsBadRequest_WhenInvalidUrl()
        {
            var extracted = new ExtractedRecipe { Title = "test" };
            _extractorMock.Setup(e => e.ExtractRecipeAsync("url", CancellationToken.None)).ReturnsAsync(extracted);
            var request = new ExtractRequest { Source = "url a" };
            var result = await _controller.ExtractRecipeAsync(request, CancellationToken.None);
            var ok = Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task ExtractRecipe_Returns500_OnException()
        {
            _extractorMock.Setup(e => e.ExtractRecipeAsync("https://url", CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var request = new ExtractRequest { Source = "url" };
            var result = await _controller.ExtractRecipeAsync(request, CancellationToken.None);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.ExtractRecipeAsync(null!, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsBadRequest_WhenSourceIsNull()
        {
            var result = await _controller.ExtractRecipeAsync(new ExtractRequest { Source = null! }, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public void CookRecipe_ReturnsBadRequest_WhenIdIsNegative()
        {
            var result = _controller.CookRecipe(-1);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public void CookRecipe_Returns500_WhenServiceThrows()
        {
            _serviceMock.Setup(s => s.CookRecipe(1, It.IsAny<int>())).Throws(new Exception("Bad id"));

            var result = _controller.CookRecipe(1);

            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public void CookRecipe_Returns500_WhenServiceReturnsNull()
        {
            _serviceMock.Setup(s => s.CookRecipe(1, It.IsAny<int>())).Returns((GetPantryItemsResult)null!);
            var result = _controller.CookRecipe(1);
            var status = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public void CookRecipe_ReturnsPantryItems_OnSuccess()
        {
            var food = new FoodDto { Id = 1, Name = "yum yum sauce", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "test" }};
            var pantryItems = new GetPantryItemsResult { TotalCount = 2, Items = [
                new PantryItemDto { Id = 1, Quantity = 1, FoodId = food.Id, Food = food },
                new PantryItemDto { Id = 2, Quantity = 3, FoodId = food.Id, Food = food }
            ] };
            _serviceMock.Setup(s => s.CookRecipe(1, It.IsAny<int>())).Returns(pantryItems);

            var result = _controller.CookRecipe(1);

            var status = Assert.IsType<OkObjectResult>(result.Result);
            var dto = Assert.IsType<GetPantryItemsResult>(status.Value);
            Assert.Equal(2, dto.TotalCount);
        }
    }
}
