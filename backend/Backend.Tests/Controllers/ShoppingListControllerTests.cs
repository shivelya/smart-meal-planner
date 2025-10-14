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
        public async Task DeleteShoppingListItemAsync_ReturnsNoContent_OnSuccess()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.DeleteShoppingListItemAsync(2, 42, System.Threading.CancellationToken.None)).ReturnsAsync(true);
            var controller = GetController(serviceMock);

            var result = await controller.DeleteShoppingListItemAsync(2);
            Assert.IsType<NoContentResult>(result.Result);
        }

        [Fact]
        public async Task DeleteShoppingListItemAsync_ReturnsBadRequest_WhenIdIsInvalid()
        {
            var controller = GetController();
            var result = await controller.DeleteShoppingListItemAsync(0);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Valid id is required.", badRequest.Value);
        }

        [Fact]
        public async Task DeleteShoppingListItemAsync_Returns500_OnException()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.DeleteShoppingListItemAsync(2, 42, System.Threading.CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(serviceMock);

            var result = await controller.DeleteShoppingListItemAsync(2);
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("fail", objResult.Value);
        }

        [Fact]
        public async Task AddShoppingListItemAsync_ReturnsOk_WithResult()
        {
            var serviceMock = new Mock<IShoppingListService>();
            var expected = new ShoppingListItemDto { Id = 2, FoodId = 20, Purchased = false, Notes = "new" };
            serviceMock.Setup(s => s.AddShoppingListItemAsync(It.IsAny<CreateUpdateShoppingListEntryRequestDto>(), 42, System.Threading.CancellationToken.None))
                .ReturnsAsync(expected);
            var controller = GetController(serviceMock);

            var request = new CreateUpdateShoppingListEntryRequestDto { FoodId = 20, Purchased = false, Notes = "new" };
            var result = await controller.AddShoppingListItemAsync(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<ShoppingListItemDto>(okResult.Value);
            Assert.Equal(2, value.Id);
            Assert.Equal(20, value.FoodId);
            Assert.False(value.Purchased);
            Assert.Equal("new", value.Notes);
        }

        [Fact]
        public async Task AddShoppingListItemAsync_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.AddShoppingListItemAsync(null!);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("request object is required.", badRequest.Value);
        }

        [Fact]
        public async Task AddShoppingListItemAsync_Returns500_OnException()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.AddShoppingListItemAsync(It.IsAny<CreateUpdateShoppingListEntryRequestDto>(), 42, System.Threading.CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));
            var controller = GetController(serviceMock);

            var request = new CreateUpdateShoppingListEntryRequestDto { FoodId = 20, Purchased = false, Notes = "new" };
            var result = await controller.AddShoppingListItemAsync(request);
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("fail", objResult.Value);
        }

        [Fact]
        public async Task UpdateShoppingList_ReturnsOk_WithResult()
        {
            var serviceMock = new Mock<IShoppingListService>();
            var expected = new ShoppingListItemDto { Id = 1, FoodId = 10, Purchased = true, Notes = "note" };
            serviceMock.Setup(s => s.UpdateShoppingListItemAsync(It.IsAny<CreateUpdateShoppingListEntryRequestDto>(), 42, System.Threading.CancellationToken.None))
                .ReturnsAsync(expected);
            var controller = GetController(serviceMock);

            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 1, FoodId = 10, Purchased = true, Notes = "note" };
            var result = await controller.UpdateShoppingListItemAsync(request);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<ShoppingListItemDto>(okResult.Value);
            Assert.Equal(1, value.Id);
            Assert.Equal(10, value.FoodId);
            Assert.True(value.Purchased);
            Assert.Equal("note", value.Notes);
        }

        [Fact]
        public async Task UpdateShoppingList_ReturnsBadRequest_WhenRequestIsNull()
        {
            var controller = GetController();
            var result = await controller.UpdateShoppingListItemAsync(null!);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("request object is required.", badRequest.Value);
        }

        [Fact]
        public async Task UpdateShoppingList_Returns500_OnException()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.UpdateShoppingListItemAsync(It.IsAny<CreateUpdateShoppingListEntryRequestDto>(), 42, System.Threading.CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));
            var controller = GetController(serviceMock);

            var request = new CreateUpdateShoppingListEntryRequestDto { Id = 1, FoodId = 10, Purchased = true, Notes = "note" };
            var result = await controller.UpdateShoppingListItemAsync(request);
            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("fail", objResult.Value);
        }

        [Fact]
        public async Task GetShoppingList_ReturnsOk_WithResult()
        {
            var serviceMock = new Mock<IShoppingListService>();
            var expected = new GetShoppingListResult
            {
                TotalCount = 2,
                Items =
                [
                    new ShoppingListItemDto { Id = 1, FoodId = 10, Purchased = false },
                    new ShoppingListItemDto { Id = 2, FoodId = 20, Purchased = true }
                ]
            };
            serviceMock.Setup(s => s.GetShoppingListAsync(42, System.Threading.CancellationToken.None)).ReturnsAsync(expected);
            var controller = GetController(serviceMock);

            var result = await controller.GetShoppingListAsync();
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<GetShoppingListResult>(okResult.Value);
            Assert.Equal(2, value.TotalCount);
            Assert.Collection(value.Items,
                item => Assert.Equal(1, item.Id),
                item => Assert.Equal(2, item.Id));
        }

        [Fact]
        public async Task GetShoppingList_Returns500_OnException()
        {
            var serviceMock = new Mock<IShoppingListService>();
            serviceMock.Setup(s => s.GetShoppingListAsync(42, System.Threading.CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var controller = GetController(serviceMock);

            var result = await controller.GetShoppingListAsync();

            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("fail", objResult.Value);
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
            serviceMock.Setup(s => s.GenerateAsync(It.IsAny<GenerateShoppingListRequestDto>(), 42, System.Threading.CancellationToken.None))
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
            serviceMock.Setup(s => s.GenerateAsync(It.IsAny<GenerateShoppingListRequestDto>(), 42, System.Threading.CancellationToken.None))
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