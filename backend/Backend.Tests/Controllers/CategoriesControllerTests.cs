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
        [Fact]
        public async Task GetCategories_ReturnsOk_WithCategories()
        {
            var serviceMock = new Mock<ICategoryService>();
            var loggerMock = new Mock<ILogger<CategoriesController>>();
            var categories = new List<CategoryDto> { new() { Id = 1, Name = "Test" } };
            var resultMock = new GetCategoriesResult { TotalCount = 1, Items = categories };
            serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(resultMock);
            var controller = new CategoriesController(serviceMock.Object, loggerMock.Object);

            var result = await controller.GetCategories();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<GetCategoriesResult>(okResult.Value);
            Assert.Equal(1, value.TotalCount);
            Assert.Single(value.Items);
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithEmptyList()
        {
            var loggerMock = new Mock<ILogger<CategoriesController>>();
            var serviceMock = new Mock<ICategoryService>();
            serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(new GetCategoriesResult { TotalCount = 0, Items = [] });
            var controller = new CategoriesController(serviceMock.Object, loggerMock.Object);
            var result = await controller.GetCategories();
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetCategoriesResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }

        [Fact]
        public async Task GetCategories_Returns500_OnException()
        {
            var serviceMock = new Mock<ICategoryService>();
            var loggerMock = new Mock<ILogger<CategoriesController>>();
            serviceMock.Setup(s => s.GetAllAsync()).ThrowsAsync(new Exception("fail"));
            var controller = new CategoriesController(serviceMock.Object, loggerMock.Object);

            var result = await controller.GetCategories();
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.NotNull(objResult.Value);
            Assert.Contains("Could not retrieve categories", objResult.Value.ToString());
        }
    }
}