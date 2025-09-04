using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Controllers
{
    public class FoodControllerTests
    {
        private readonly Mock<IFoodService> _serviceMock = new();
        private readonly Mock<ILogger<FoodController>> _loggerMock = new();
        private readonly FoodController _controller;

        public FoodControllerTests()
        {
            _controller = new FoodController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SearchFoods_ReturnsBadRequest_WhenSearchIsNullOrWhitespace()
        {
            var result = await _controller.SearchFoods("");
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Search term is required.", badRequest.Value);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithResults()
        {
            var foods = new List<FoodDto> { new() { Id = 1, Name = "Salt", Category = new CategoryDto { Id = 1, Name = "produce" }},
                new() { Id = 2, Name = "Pepper", Category = new CategoryDto { Id = 1, Name = "produce" }}};
            _serviceMock.Setup(s => s.SearchFoods("spice")).ReturnsAsync(foods);

            var result = await _controller.SearchFoods("spice");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Equal(2, returned.TotalCount);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithEmptyResults()
        {
            _serviceMock.Setup(s => s.SearchFoods("none")).ReturnsAsync([]);

            var result = await _controller.SearchFoods("none");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }
    }
}
