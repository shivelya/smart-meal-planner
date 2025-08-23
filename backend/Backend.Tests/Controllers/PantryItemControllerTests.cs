using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers
{
    public class PantryItemControllerTests
    {
        private readonly Mock<IPantryItemService> _serviceMock;
        private readonly Mock<ILogger<PantryItemController>> _loggerMock;
        private readonly PantryItemController _controller;

        public PantryItemControllerTests()
        {
            _serviceMock = new Mock<IPantryItemService>();
            _loggerMock = new Mock<ILogger<PantryItemController>>();
            _controller = new PantryItemController(_serviceMock.Object, _loggerMock.Object);
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "42")
            }, "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task AddItem_ReturnsOk_WithCreatedItem()
        {
            var dto = new CreatePantryItemDto { IngredientId = 1, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, IngredientId = 1, Quantity = 2, Unit = "kg", UserId = 42 };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItems_ReturnsOk_WithCreatedItems()
        {
            var dtos = new List<CreatePantryItemDto> { new CreatePantryItemDto { IngredientId = 1, Quantity = 2, Unit = "kg" } };
            var resultDtos = new List<PantryItemDto> { new PantryItemDto { Id = 1, IngredientId = 1, Quantity = 2, Unit = "kg", UserId = 42 } };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(dtos, 42)).ReturnsAsync(resultDtos);

            var result = await _controller.AddItems(dtos);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDtos, okResult.Value);
        }

        [Fact]
        public async Task GetItem_ItemExists_ReturnsOk()
        {
            var resultDto = new PantryItemDto { Id = 1, IngredientId = 1, Quantity = 2, Unit = "kg", UserId = 42 };
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1)).ReturnsAsync(resultDto);

            var result = await _controller.GetItem(1);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task GetItem_ItemDoesNotExist_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1)).ReturnsAsync((PantryItemDto)null!);

            var result = await _controller.GetItem(1);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetItems_ReturnsOk_WithItemsAndTotalCount()
        {
            var items = new List<PantryItemDto> { new PantryItemDto { Id = 1, IngredientId = 1, Quantity = 2, Unit = "kg", UserId = 42 } };
            _serviceMock.Setup(s => s.GetAllPantryItemsAsync(1, 10)).ReturnsAsync((items, items.Count));

            var result = await _controller.GetItems(1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            GetItemsResult value = Assert.IsType<GetItemsResult>(okResult.Value);
            Assert.Equal(items.Count, value.TotalCount);
            Assert.Equal(items, value.Items);
        }

        [Fact]
        public async Task DeleteItem_ItemExists_ReturnsNoContent()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(1)).ReturnsAsync(true);

            var result = await _controller.DeleteItem(1);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteItem_ItemDoesNotExist_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(1)).ReturnsAsync(false);

            var result = await _controller.DeleteItem(1);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteItems_ItemsDeleted_ReturnsOk()
        {
            var ids = new List<int> { 1, 2 };
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(ids)).ReturnsAsync(2);

            var result = await _controller.DeleteItems(ids);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(2, okResult.Value);
        }

        [Fact]
        public async Task DeleteItems_NoItemsDeleted_ReturnsNotFound()
        {
            var ids = new List<int> { 1, 2 };
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(ids)).ReturnsAsync(0);

            var result = await _controller.DeleteItems(ids);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task AddItem_WithIngredientId_CreatesItem()
        {
            var dto = new CreatePantryItemDto { IngredientId = 5, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, IngredientId = 5, Quantity = 2, Unit = "kg", UserId = 42 };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithIngredientName_CreatesItem()
        {
            var dto = new CreatePantryItemDto { IngredientName = "Salt", Quantity = 1, Unit = "g" };
            var resultDto = new PantryItemDto { Id = 2, IngredientId = 10, Quantity = 1, Unit = "g", UserId = 42 };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithoutIngredientIdOrName_ReturnsBadRequest()
        {
            var dto = new CreatePantryItemDto { Quantity = 1, Unit = "g" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ThrowsAsync(new ArgumentException());

            var result = await _controller.AddItem(dto);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task AddItems_MixedDtos_FiltersAndCreatesValid()
        {
            var dtos = new List<CreatePantryItemDto>
            {
                new CreatePantryItemDto { IngredientId = 1, Quantity = 2, Unit = "kg" },
                new CreatePantryItemDto { IngredientName = "Sugar", Quantity = 3, Unit = "g" },
                new CreatePantryItemDto { Quantity = 5, Unit = "g" } // invalid
            };
            var resultDtos = new List<PantryItemDto>
            {
                new PantryItemDto { Id = 1, IngredientId = 1, Quantity = 2, Unit = "kg", UserId = 42 },
                new PantryItemDto { Id = 2, IngredientId = 11, Quantity = 3, Unit = "g", UserId = 42 }
            };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(It.IsAny<IEnumerable<CreatePantryItemDto>>(), 42)).ReturnsAsync(resultDtos);

            var result = await _controller.AddItems(dtos);
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDtos, okResult.Value);
        }
    }
}
