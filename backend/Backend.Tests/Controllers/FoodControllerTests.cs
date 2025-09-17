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
        public async Task SearchFoods_Returns_WhenSearchIsNullOrWhitespace()
        {
            _serviceMock.Setup(s => s.SearchFoodsAsync("", It.IsAny<int?>(), It.IsAny<int?>(), System.Threading.CancellationToken.None)).ReturnsAsync(new GetFoodsResult { Items = [], TotalCount = 0 });
            var result = await _controller.SearchFoodsAsync("", System.Threading.CancellationToken.None);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithResults()
        {
            var foods = new List<FoodDto> { new() { Id = 1, Name = "Salt", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce" }},
                new() { Id = 2, Name = "Pepper", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce" }}};
            _serviceMock.Setup(s => s.SearchFoodsAsync("spice", null, 50, System.Threading.CancellationToken.None)).ReturnsAsync(new GetFoodsResult { TotalCount = foods.Count, Items = foods });

            var result = await _controller.SearchFoodsAsync("spice", System.Threading.CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Equal(2, returned.TotalCount);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithEmptyResults()
        {
            _serviceMock.Setup(s => s.SearchFoodsAsync("none", null, 50, System.Threading.CancellationToken.None)).ReturnsAsync(new GetFoodsResult { TotalCount = 0, Items = [] });

            var result = await _controller.SearchFoodsAsync("none", System.Threading.CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }
    }
}
