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
    public class CategoriesControllerTests
    {
        [Fact]
        public async Task GetCategories_ReturnsOk_WithCategories()
        {
            var serviceMock = new Mock<ICategoryService>();
            var loggerMock = new Mock<ILogger<CategoryController>>();
            var categories = new List<CategoryDto> { new() { Id = 1, Name = "Test" } };
            var resultMock = new GetCategoriesResult { TotalCount = 1, Items = categories };
            serviceMock.Setup(s => s.GetAllAsync(CancellationToken.None)).ReturnsAsync(resultMock);
            var controller = new CategoryController(serviceMock.Object, loggerMock.Object);

            var rand = new Random();
            var userId = rand.Next(1, 1000);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await controller.GetCategories(CancellationToken.None);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<GetCategoriesResult>(okResult.Value);
            Assert.Equal(1, value.TotalCount);
            Assert.Single(value.Items);
        }

        [Fact]
        public async Task GetCategories_ReturnsOk_WithEmptyList()
        {
            var loggerMock = new Mock<ILogger<CategoryController>>();
            var serviceMock = new Mock<ICategoryService>();
            serviceMock.Setup(s => s.GetAllAsync(CancellationToken.None)).ReturnsAsync(new GetCategoriesResult { TotalCount = 0, Items = [] });
            var controller = new CategoryController(serviceMock.Object, loggerMock.Object);

            var rand = new Random();
            var userId = rand.Next(1, 1000);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await controller.GetCategories(CancellationToken.None);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetCategoriesResult>(ok.Value);
            Assert.Empty(returned.Items);
            Assert.Equal(0, returned.TotalCount);
        }

        [Fact]
        public async Task GetCategories_Returns500_OnException()
        {
            var serviceMock = new Mock<ICategoryService>();
            var loggerMock = new Mock<ILogger<CategoryController>>();
            serviceMock.Setup(s => s.GetAllAsync(CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = new CategoryController(serviceMock.Object, loggerMock.Object);

            var rand = new Random();
            var userId = rand.Next(1, 1000);
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            ], "mock"));
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };

            var result = await controller.GetCategories(CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }
    }
}