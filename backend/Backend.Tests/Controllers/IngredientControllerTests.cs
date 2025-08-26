using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
        public async Task SearchIngredients_ReturnsOk_WithResults()
        {
            var ingredients = new List<IngredientDto> { new IngredientDto { Id = 1, Name = "Salt", Category = new CategoryDto { Name = "produce" }},
                new IngredientDto { Id = 2, Name = "Pepper", Category = new CategoryDto { Name = "produce" }}};
            _serviceMock.Setup(s => s.SearchIngredients("spice")).ReturnsAsync(ingredients);
            var result = await _controller.SearchIngredients("spice");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<IngredientDto>>(ok.Value);
            Assert.Equal(2, returned.Count());
        }

        [Fact]
        public async Task SearchIngredients_ReturnsOk_WithEmptyResults()
        {
            _serviceMock.Setup(s => s.SearchIngredients("none")).ReturnsAsync(new List<IngredientDto>());
            var result = await _controller.SearchIngredients("none");
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsAssignableFrom<IEnumerable<IngredientDto>>(ok.Value);
            Assert.Empty(returned);
        }
    }
}
