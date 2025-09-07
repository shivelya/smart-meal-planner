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
            _serviceMock.Setup(s => s.CreateAsync(dto, 1)).ReturnsAsync(resultDto);
            var result = await _controller.Create(dto);
            var created = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDto, created.Value);
        }

        [Fact]
        public async Task Create_Returns500_OnException()
        {
            var dto = new Mock<CreateUpdateRecipeDtoRequest>().Object;
            _serviceMock.Setup(s => s.CreateAsync(dto, 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.Create(dto);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenFound()
        {
            var recipe = new RecipeDto { Id = 2 };
            _serviceMock.Setup(s => s.GetByIdAsync(2, 1)).ReturnsAsync(recipe);
            var result = await _controller.GetById(2);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(recipe, ok.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenNull()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(3, 1)).ReturnsAsync((RecipeDto?)null);
            var result = await _controller.GetById(3);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetById_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.GetByIdAsync(4, 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetById(4);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsOk_WhenSuccess()
        {
            var ids = new[] { 1, 2 };
            var recipesRequest = new GetRecipesRequest { Ids = ids };
            var recipes = new List<RecipeDto> { new() { Id = 1 }, new() { Id = 2 } };
            var recipesResult = new GetRecipesResult { TotalCount = 2, Items = recipes };
            _serviceMock.Setup(s => s.GetByIdsAsync(ids, 1)).ReturnsAsync(recipesResult);
            var result = await _controller.GetByIds(recipesRequest);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var r = Assert.IsType<GetRecipesResult>(ok.Value);
            Assert.Equal(recipes, r.Items);
        }

        [Fact]
        public async Task GetByIds_Returns500_OnException()
        {
            var ids = new[] { 1, 2 };
            var recipesRequest = new GetRecipesRequest { Ids = ids };
            _serviceMock.Setup(s => s.GetByIdsAsync(ids, 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GetByIds(recipesRequest);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsOk_WhenSuccess()
        {
            var recipes = new List<RecipeDto> { new RecipeDto { Id = 1 } };
            var recipesResult = new GetRecipesResult { TotalCount = 1, Items = recipes };
            _serviceMock.Setup(s => s.SearchAsync(It.IsAny<RecipeSearchOptions>(), 1)).ReturnsAsync(recipesResult);
            var result = await _controller.Search("Pizza", null, 0, 10);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var r = Assert.IsType<GetRecipesResult>(ok.Value);
            Assert.Equal(recipes, r.Items);
        }

        [Fact]
        public async Task Search_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.SearchAsync(It.IsAny<RecipeSearchOptions>(), 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.Search("Pizza", null, 0, 10);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsOk_WhenSuccess()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Id = 1, Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            var updated = new RecipeDto { Id = 1 };
            _serviceMock.Setup(s => s.UpdateAsync(1, dto, 1)).ReturnsAsync(updated);
            var result = await _controller.Update(1, dto);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(updated, ok.Value);
        }

        [Fact]
        public async Task Update_ReturnsNotFound_WhenNull()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Id = 2, Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            _serviceMock.Setup(s => s.UpdateAsync(2, dto, 1)).ReturnsAsync((RecipeDto)null!);
            var result = await _controller.Update(2, dto);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task Update_Returns500_OnException()
        {
            var dto = new CreateUpdateRecipeDtoRequest { Id = 3, Title = "T", Source = "S", Instructions = "I", Ingredients = new List<CreateUpdateRecipeIngredientDto>() };
            _serviceMock.Setup(s => s.UpdateAsync(3, dto, 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.Update(3, dto);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task Delete_ReturnsNoContent_WhenSuccess()
        {
            _serviceMock.Setup(s => s.DeleteAsync(1, 1)).ReturnsAsync(true);
            var result = await _controller.Delete(1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task Delete_ReturnsNotFound_WhenFalse()
        {
            _serviceMock.Setup(s => s.DeleteAsync(2, 1)).ReturnsAsync(false);
            var result = await _controller.Delete(2);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.DeleteAsync(3, 1)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.Delete(3);
            var status = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsOk_WhenSuccess()
        {
            var extracted = new ExtractedRecipe { Title = "test" };
            _extractorMock.Setup(e => e.ExtractRecipeAsync("url")).ReturnsAsync(extracted);
            var request = new ExtractRequest { Source = "url" };
            var result = await _controller.ExtractRecipe(request);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(extracted, ok.Value);
        }

        [Fact]
        public async Task ExtractRecipe_Returns500_OnException()
        {
            _extractorMock.Setup(e => e.ExtractRecipeAsync("url")).ThrowsAsync(new Exception("fail"));
            var request = new ExtractRequest { Source = "url" };
            var result = await _controller.ExtractRecipe(request);
            var status = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, status.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.GetByIds(null!);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task GetByIds_ReturnsBadRequest_WhenIdsIsNull()
        {
            var result = await _controller.GetByIds(new GetRecipesRequest { Ids = null! });
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.ExtractRecipe(null!);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task ExtractRecipe_ReturnsBadRequest_WhenSourceIsNull()
        {
            var result = await _controller.ExtractRecipe(new ExtractRequest { Source = null! });
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }
    }
}
