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
    public class ShoppingListControllerTests
    {
        private static ShoppingListController GetController(Mock<IShoppingListService> serviceMock = null!)
        {
            var loggerMock = new Mock<ILogger<ShoppingListController>>();
            var controller = new ShoppingListController(serviceMock?.Object ?? Mock.Of<IShoppingListService>(), loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "42")]));
            controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
            return controller;
        }

        [Fact]
        public async Task GenerateAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.GenerateAsync(null!);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("request object is required.", badRequest.Value);
        }

        [Fact]
        public async Task GenerateAsync_ReturnsBadRequest_WhenMealPlanIdIsInvalid()
        {
            var controller = GetController();
            var request = new GenerateShoppingListRequestDto { MealPlanId = 0, Restart = false };
            var result = await controller.GenerateAsync(request);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Valid meal plan id is required.", badRequest.Value);
        }

        [Fact]
        public async Task GenerateAsync_ReturnsOk_WhenServiceSucceeds()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.GenerateAsync(It.IsAny<GenerateShoppingListRequestDto>(), 42))
                .Returns(Task.CompletedTask);
            var controller = GetController(serviceMock);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 1, Restart = false };
            var result = await controller.GenerateAsync(request);
            Assert.IsType<OkResult>(result.Result);
        }

        [Fact]
        public async Task GenerateAsync_Returns500_WhenServiceThrows()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.GenerateAsync(It.IsAny<GenerateShoppingListRequestDto>(), 42))
                .ThrowsAsync(new Exception("fail"));
            var controller = GetController(serviceMock);

            var request = new GenerateShoppingListRequestDto { MealPlanId = 1, Restart = false };
            var result = await controller.GenerateAsync(request);
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("fail", objResult.Value);
        }
    }
}