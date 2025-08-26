using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Controllers
{
    public class CategoriesControllerTests
    {
        private readonly Mock<ICategoryService> _serviceMock = new();
        private readonly Mock<ILogger<CategoriesController>> _loggerMock = new();
        private readonly CategoriesController _controller;

        public CategoriesControllerTests()
        {
            _controller = new CategoriesController(_serviceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithCategories()
        {
            var categories = new List<CategoryDto> { new CategoryDto { Id = 1, Name = "Fruit" }, new CategoryDto { Id = 2, Name = "Vegetable" } };
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(categories);
            var result = await _controller.GetCategories();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithEmptyList()
        {
            _serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new List<CategoryDto>());
            var result = await _controller.GetCategories();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<CategoryDto>>(ok.Value);
            Assert.Empty(returned);
        }
    }
}
