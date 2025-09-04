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
            var user = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, "42")
            ], "mock"));
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        [Fact]
        public async Task AddItem_ReturnsOk_WithCreatedItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);

            var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItems_ReturnsOk_WithCreatedItems()
        {
            var dtos = new List<CreateUpdatePantryItemRequestDto> { new() { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" } };
            var resultDtos = new List<PantryItemDto> { new() { Id = 1, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" } };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(dtos, 42)).ReturnsAsync(resultDtos);

            var result = await _controller.AddItems(dtos);

            var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDtos, okResult.Value);
        }

        [Fact]
        public async Task GetItem_ItemExists_ReturnsOk()
        {
            var resultDto = new PantryItemDto { Id = 1, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" };
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
            var items = new List<PantryItemDto> { new() { Id = 1, Food = new FoodDto { Id = 1, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} },
                Quantity = 2, Unit = "kg" } };
            _serviceMock.Setup(s => s.GetAllPantryItemsAsync(1, 10)).ReturnsAsync((items, items.Count));

            var result = await _controller.GetItems(1, 10);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            GetPantryItemsResult value = Assert.IsType<GetPantryItemsResult>(okResult.Value);
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
            var deleteRequest = new DeleteRequest { Ids = ids };
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(ids)).ReturnsAsync(new DeleteRequest { Ids = [2] });

            var result = await _controller.DeleteItems(deleteRequest);

            var okResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(204, okResult.StatusCode);
            var deleteResult = Assert.IsType<DeleteRequest>(okResult.Value);
            Assert.Equal(2, deleteRequest.Ids.Count());
        }

        [Fact]
        public async Task DeleteItems_NoItemsDeleted_ReturnsNotFound()
        {
            var ids = new List<int> { 1, 2 };
            var deleteRequest = new DeleteRequest { Ids = ids };
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(ids)).ReturnsAsync(new DeleteRequest { Ids = [] });

            var result = await _controller.DeleteItems(deleteRequest);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AddItem_WithFoodId_CreatesItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 5 }, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, Food = new FoodDto { Id = 1, Name ="banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);
            var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithFoodName_CreatesItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Salt", CategoryId = 1 }, Quantity = 1, Unit = "g" };
            var resultDto = new PantryItemDto { Id = 2, Food = new FoodDto { Id = 1, Name = "salt", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 1, Unit = "g" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ReturnsAsync(resultDto);

            var result = await _controller.AddItem(dto);
            var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithoutFoodIdOrName_ReturnsBadRequest()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Unit = "g", Food = new TestFoodReferenceDto() };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, 42)).ThrowsAsync(new ArgumentException());

            var result = await _controller.AddItem(dto);
            var resulta = Assert.IsType<ObjectResult>(result.Result);
            Assert.IsType<ArgumentException>(resulta.Value);
            Assert.IsType<ArgumentException>(resulta.Value);
        }

        public class TestFoodReferenceDto : FoodReferenceDto { }

        [Fact]
        public async Task AddItems_MixedDtos_FiltersAndCreatesValid()
        {
            var dtos = new List<CreateUpdatePantryItemRequestDto>
            {
                new() { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" },
                new() { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Sugar", CategoryId = 1 }, Quantity = 3, Unit = "g" },
                new() { Quantity = 5, Unit = "g", Food = new TestFoodReferenceDto() } // invalid
            };
            var resultDtos = new List<PantryItemDto>
            {
                new() { Id = 1, Food = new FoodDto { Id = 1, Name = "tomato", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" },
                new() { Id = 2, Food = new FoodDto { Id = 2, Name = "banana", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 3, Unit = "g" }
            };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(It.IsAny<IEnumerable<CreateUpdatePantryItemRequestDto>>(), 42)).ReturnsAsync(resultDtos);

            var result = await _controller.AddItems(dtos);
            var okResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultDtos, okResult.Value);
        }

        [Fact]
        public async Task Search_WithValidTerm_ReturnsOkWithResults()
        {
            var searchTerm = "Salt";
            var expectedResults = new List<PantryItemDto>
            {
                new() { Id = 1, Food = new FoodDto { Id = 1, Name = "tomato", Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "g" }
            };
            _serviceMock.Setup(s => s.Search(searchTerm, 42)).ReturnsAsync(expectedResults);
            var searchRequest = new PantrySearchRequest { Query = searchTerm };

            var result = await _controller.Search(searchRequest);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetPantryItemsResult>(okResult.Value);
            Assert.Equal(expectedResults, returned.Items);
            Assert.Equal(expectedResults.Count(), returned.TotalCount);
        }

        [Fact]
        public async Task Search_WithEmptyTerm_ReturnsBadRequest()
        {
            var request = new PantrySearchRequest { Query = "" };
            var result = await _controller.Search(request);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("A search term is required.", badRequest.Value);
        }

        [Fact]
        public async Task Search_WithNullTerm_ReturnsBadRequest()
        {
            var result = await _controller.Search(null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("A search term is required.", badRequest.Value);
        }

        [Fact]
        public async Task Search_ReturnsEmptyListIfNoResults()
        {
            var searchTerm = "NotFound";
            _serviceMock.Setup(s => s.Search(searchTerm, 42)).ReturnsAsync([]);
            var request = new PantrySearchRequest { Query = searchTerm };

            var result = await _controller.Search(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<GetPantryItemsResult>(okResult.Value, exactMatch: false);
            Assert.Empty(value.Items);
            Assert.Equal(0, value.TotalCount);
        }

        [Fact]
        public async Task Update_WithValidIdAndDto_ReturnsServiceResult()
        {
            var pantryItemDto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
            var updatedDto = new PantryItemDto { Id = 1, Food = new FoodDto { Id = 1, Name = "", Category = new CategoryDto { Id = 1, Name = "produce "}},
                Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.UpdatePantryItemAsync(pantryItemDto, 42)).ReturnsAsync(updatedDto);

            var result = await _controller.Update("1", pantryItemDto);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(updatedDto, okResult.Value);
        }

        [Fact]
        public async Task Update_WithNullId_ReturnsBadRequest()
        {
            var pantryItemDto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };

            var result = await _controller.Update(null!, pantryItemDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("id is required.", badRequest.Value);
        }

        [Fact]
        public async Task Update_WithEmptyId_ReturnsBadRequest()
        {
            var pantryItemDto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };

            var result = await _controller.Update("", pantryItemDto);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("id is required.", badRequest.Value);
        }

        [Fact]
        public async Task Update_WithNullDto_ReturnsBadRequest()
        {
            var result = await _controller.Update("1", null!);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("PantryItemDto pantryItem is required.", badRequest.Value);
        }
    }
}
