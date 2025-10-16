using System.Security.Claims;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Http;
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
             var rand = new Random();
            var userId = rand.Next(1, 1000);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task SearchFoods_Returns_WhenSearchIsNullOrWhitespace()
        {
            _serviceMock.Setup(s => s.SearchFoodsAsync("", It.IsAny<int?>(), It.IsAny<int?>(), CancellationToken.None)).ReturnsAsync(new GetFoodsResult { Items = [], TotalCount = 0 });
            var result = await _controller.SearchFoodsAsync("", CancellationToken.None);
            Assert.IsType<OkObjectResult>(result.Result);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithResults()
        {
            var foods = new List<FoodDto> { new() { Id = 1, Name = "Salt", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce" }},
                new() { Id = 2, Name = "Pepper", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce" }}};
            _serviceMock.Setup(s => s.SearchFoodsAsync("spice", null, 50, CancellationToken.None)).ReturnsAsync(new GetFoodsResult { TotalCount = foods.Count, Items = foods });

            var result = await _controller.SearchFoodsAsync("spice", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Equal(2, returned.TotalCount);
        }

        [Fact]
        public async Task SearchFoods_ReturnsOk_WithEmptyResults()
        {
            _serviceMock.Setup(s => s.SearchFoodsAsync("none", null, 50, CancellationToken.None)).ReturnsAsync(new GetFoodsResult { TotalCount = 0, Items = [] });

            var result = await _controller.SearchFoodsAsync("none", CancellationToken.None);

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetFoodsResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }

        [Fact]
        public async Task SearchFoods_ReturnsBadRequest_WhenSkipIsNegative()
        {
            var result = await _controller.SearchFoodsAsync("test", CancellationToken.None, skip: -1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Skip must be greater than or equal to zero.", badRequest.Value);
        }

        [Fact]
        public async Task SearchFoods_ReturnsBadRequest_WhenTakeIsZeroOrNegative()
        {
            var resultZero = await _controller.SearchFoodsAsync("test", CancellationToken.None, take: 0);
            var badRequestZero = Assert.IsType<BadRequestObjectResult>(resultZero.Result);
            Assert.Equal("Take must be greater than zero.", badRequestZero.Value);

            var resultNegative = await _controller.SearchFoodsAsync("test", CancellationToken.None, take: -5);
            var badRequestNegative = Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
            Assert.Equal("Take must be greater than zero.", badRequestNegative.Value);
        }
    }
}
