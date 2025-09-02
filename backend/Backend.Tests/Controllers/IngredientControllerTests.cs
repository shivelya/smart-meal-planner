using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Controllers
{
    public class IngredientControllerTests
    {
        private readonly Mock<IIngredientService> _serviceMock = new();
        private readonly Mock<ILogger<IngredientController>> _loggerMock = new();
        private readonly IngredientController _controller;

        public IngredientControllerTests()
        {
            _controller = new IngredientController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SearchIngredients_ReturnsBadRequest_WhenSearchIsNullOrWhitespace()
        {
            var result = await _controller.SearchIngredients("");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search term is required.", badRequest.Value);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithResults()
        {
            var foods = new List<FoodReferenceDto> { new() { Id = 1, Name = "Salt", Category = new CategoryDto { Name = "produce" }},
                new() { Id = 2, Name = "Pepper", Category = new CategoryDto { Name = "produce" }}};
            _serviceMock.Setup(s => s.SearchIngredients("spice")).ReturnsAsync(foods);

            var result = await _controller.SearchIngredients("spice");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Equal(2, returned.TotalCount);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithEmptyResults()
        {
            _serviceMock.Setup(s => s.SearchIngredients("none")).ReturnsAsync([]);

            var result = await _controller.SearchIngredients("none");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }
    }
}
