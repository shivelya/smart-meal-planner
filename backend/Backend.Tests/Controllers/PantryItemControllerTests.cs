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
        private readonly int userId;

        public PantryItemControllerTests()
        {
            _serviceMock = new Mock<IPantryItemService>();
            _loggerMock = new Mock<ILogger<PantryItemController>>();
            _controller = new PantryItemController(_serviceMock.Object, _loggerMock.Object);
            var rand = new Random();
            userId = rand.Next(1, 1000);
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
        public async Task AddItem_ReturnsOk_WithCreatedItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "banana", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " } }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, userId, CancellationToken.None)).ReturnsAsync(resultDto);

            var result = await _controller.AddItemAsync(dto, CancellationToken.None);

            var okResult = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

            [Fact]
            public async Task AddItem_Returns500_WhenServiceReturnsNull()
            {
                var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
                _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, userId, CancellationToken.None)).ReturnsAsync((PantryItemDto)null!);
                var result = await _controller.AddItemAsync(dto, CancellationToken.None);
                var objResult = Assert.IsType<StatusCodeResult>(result.Result);
                Assert.Equal(500, objResult.StatusCode);
            }

        [Fact]
        public async Task AddItems_ReturnsOk_WithCreatedItems()
        {
            var dtos = new List<CreateUpdatePantryItemRequestDto> { new()
            {
                Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 },
                Quantity = 2,
                Unit = "kg"
            }};

            var resultDtos = new List<PantryItemDto> { new()
            {
                Id = 1,
                FoodId = 1,
                Food = new FoodDto
                {
                    Id = 1,
                    Name = "banana",
                    CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " }
                },
                Quantity = 2,
                Unit = "kg"
            }};

            _serviceMock.Setup(s => s.CreatePantryItemsAsync(dtos, userId, CancellationToken.None))
                .ReturnsAsync(new GetPantryItemsResult{ TotalCount = resultDtos.Count, Items = resultDtos });

            var result = await _controller.AddItemsAsync(dtos, CancellationToken.None);

            var okResult = Assert.IsType<CreatedResult>(result.Result);
            var dto = Assert.IsType<GetPantryItemsResult>(okResult.Value);
            Assert.Equal(resultDtos, dto.Items);
        }

        [Fact]
        public async Task AddItems_Returns500_WhenServiceReturnsNull()
        {
            var dtos = new List<CreateUpdatePantryItemRequestDto> { new() { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" } };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(dtos, userId, CancellationToken.None)).ReturnsAsync((GetPantryItemsResult)null!);
            var result = await _controller.AddItemsAsync(dtos, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetItem_ItemExists_ReturnsOk()
        {
            var resultDto = new PantryItemDto { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "banana", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " } }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1, CancellationToken.None)).ReturnsAsync(resultDto);

            var result = await _controller.GetItemAsync(1, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task GetItem_ItemDoesNotExist_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1, CancellationToken.None)).ReturnsAsync((PantryItemDto)null!);

            var result = await _controller.GetItemAsync(1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetItems_ReturnsOk_WithItemsAndTotalCount()
        {
            var items = new List<PantryItemDto> { new() { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "banana", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce "} },
                Quantity = 2, Unit = "kg" } };
            _serviceMock.Setup(s => s.GetAllPantryItemsAsync(It.IsAny<int>(), 1, 10, CancellationToken.None)).ReturnsAsync(new GetPantryItemsResult { TotalCount = items.Count, Items = items });

            var result = await _controller.GetItemsAsync(1, 10, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            GetPantryItemsResult value = Assert.IsType<GetPantryItemsResult>(okResult.Value);
            Assert.Equal(items.Count, value.TotalCount);
            Assert.Equal(items, value.Items);
        }

        [Fact]
        public async Task GetItems_Returns500_WhenServiceReturnsNull()
        {
            _serviceMock.Setup(s => s.GetAllPantryItemsAsync(It.IsAny<int>(), 1, 10, CancellationToken.None)).ReturnsAsync((GetPantryItemsResult)null!);
            var result = await _controller.GetItemsAsync(1, 10, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_ItemExists_ReturnsNoContent()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(It.IsAny<int>(), 1, CancellationToken.None)).ReturnsAsync(true);

            var result = await _controller.DeleteItemAsync(1, CancellationToken.None);

            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteItem_ItemDoesNotExist_ReturnsNotFound()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(It.IsAny<int>(), 1, CancellationToken.None)).ReturnsAsync(false);

            var result = await _controller.DeleteItemAsync(1, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteItems_ItemsDeleted_ReturnsOk()
        {
            var ids = new List<int> { 1, 2 };
            var deleteRequest = new DeleteRequest { Ids = ids };
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(It.IsAny<int>(), ids, CancellationToken.None)).ReturnsAsync(new DeleteRequest { Ids = [2] });

            var result = await _controller.DeleteItemsAsync(deleteRequest, CancellationToken.None);

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
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(It.IsAny<int>(), ids, CancellationToken.None)).ReturnsAsync(new DeleteRequest { Ids = [] });

            var result = await _controller.DeleteItemsAsync(deleteRequest, CancellationToken.None);

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AddItem_WithFoodId_CreatesItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 5 }, Quantity = 2, Unit = "kg" };
            var resultDto = new PantryItemDto { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "banana", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " } }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, userId, CancellationToken.None)).ReturnsAsync(resultDto);

            var result = await _controller.AddItemAsync(dto, CancellationToken.None);
            var okResult = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithFoodName_CreatesItem()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Food = new NewFoodReferenceDto { Mode = AddFoodMode.New, Name = "Salt", CategoryId = 1 }, Quantity = 1, Unit = "g" };
            var resultDto = new PantryItemDto { Id = 2, FoodId = 1, Food = new FoodDto { Id = 1, Name = "salt", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " } }, Quantity = 1, Unit = "g" };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, userId, CancellationToken.None)).ReturnsAsync(resultDto);

            var result = await _controller.AddItemAsync(dto, CancellationToken.None);
            var okResult = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(resultDto, okResult.Value);
        }

        [Fact]
        public async Task AddItem_WithoutFoodIdOrName_ReturnsBadRequest()
        {
            var dto = new CreateUpdatePantryItemRequestDto { Quantity = 1, Unit = "g", Food = new TestFoodReferenceDto() };
            _serviceMock.Setup(s => s.CreatePantryItemAsync(dto, userId, CancellationToken.None)).ThrowsAsync(new ArgumentException("test message"));

            var result = await _controller.AddItemAsync(dto, CancellationToken.None);
            var resulta = Assert.IsType<BadRequestResult>(result.Result);
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
                new() { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "tomato", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "kg" },
                new() { Id = 2, FoodId = 2, Food = new FoodDto { Id = 2, Name = "banana", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 3, Unit = "g" }
            };
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(It.IsAny<IEnumerable<CreateUpdatePantryItemRequestDto>>(), userId, CancellationToken.None))
                .ReturnsAsync(new GetPantryItemsResult { TotalCount = resultDtos.Count, Items = resultDtos });

            var result = await _controller.AddItemsAsync(dtos, CancellationToken.None);
            var okResult = Assert.IsType<CreatedResult>(result.Result);
            var dto = Assert.IsType<GetPantryItemsResult>(okResult.Value);
            Assert.Equal(resultDtos, dto.Items);
        }

        [Fact]
        public async Task Search_WithValidTerm_ReturnsOkWithResults()
        {
            var searchTerm = "Salt";
            var expectedResults = new List<PantryItemDto>
            {
                new() { Id = 1, FoodId = 1, Food = new FoodDto { Id = 1, Name = "tomato", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce "} }, Quantity = 2, Unit = "g" }
            };

            _serviceMock.Setup(s => s.Search(searchTerm, userId, null, null, CancellationToken.None)).ReturnsAsync(new GetPantryItemsResult { TotalCount = expectedResults.Count, Items = expectedResults });

            var result = await _controller.SearchAsync(searchTerm, null, null, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var returned = Assert.IsType<GetPantryItemsResult>(okResult.Value);
            Assert.Equal(expectedResults, returned.Items);
            Assert.Equal(expectedResults.Count(), returned.TotalCount);
        }

        [Fact]
        public async Task Search_Returns500_WhenServiceReturnsNull()
        {
            var searchTerm = "Salt";
            _serviceMock.Setup(s => s.Search(searchTerm, userId, null, null, CancellationToken.None)).ReturnsAsync((GetPantryItemsResult)null!);
            var result = await _controller.SearchAsync(searchTerm, null, null, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task Search_WithEmptyTerm_ReturnsBadRequest()
        {
            var result = await _controller.SearchAsync("", null, null, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("required", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task Search_WithNullTerm_ReturnsBadRequest()
        {
            var result = await _controller.SearchAsync(null!, null, null, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("required", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task Search_ReturnsEmptyListIfNoResults()
        {
            var searchTerm = "NotFound";
            _serviceMock.Setup(s => s.Search(searchTerm, userId, null, null, CancellationToken.None)).ReturnsAsync(new GetPantryItemsResult { TotalCount = 0, Items = [] });

            var result = await _controller.SearchAsync(searchTerm, null, null, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var value = Assert.IsType<GetPantryItemsResult>(okResult.Value, exactMatch: false);
            Assert.Empty(value.Items);
            Assert.Equal(0, value.TotalCount);
        }

        [Fact]
        public async Task Update_WithValidIdAndDto_ReturnsServiceResult()
        {
            var pantryItemDto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
            var updatedDto = new PantryItemDto
            {
                Id = 1,
                FoodId = 1,
                Food = new FoodDto { Id = 1, Name = "", CategoryId = 1, Category = new CategoryDto { Id = 1, Name = "produce " } },
                Quantity = 2,
                Unit = "kg"
            };
            _serviceMock.Setup(s => s.UpdatePantryItemAsync(pantryItemDto, userId, CancellationToken.None)).ReturnsAsync(updatedDto);

            var result = await _controller.UpdateAsync(1, pantryItemDto, CancellationToken.None);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(updatedDto, okResult.Value);
        }

        [Fact]
        public async Task Update_Returns500_WhenServiceReturnsNull()
        {
            var pantryItemDto = new CreateUpdatePantryItemRequestDto { Food = new ExistingFoodReferenceDto { Mode = AddFoodMode.Existing, Id = 1 }, Quantity = 2, Unit = "kg" };
            _serviceMock.Setup(s => s.UpdatePantryItemAsync(pantryItemDto, userId, CancellationToken.None)).ReturnsAsync((PantryItemDto)null!);
            var result = await _controller.UpdateAsync(1, pantryItemDto, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task Update_WithNullDto_ReturnsBadRequest()
        {
            var result = await _controller.UpdateAsync(1, null!, CancellationToken.None);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Contains("required", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task AddItem_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.CreatePantryItemAsync(It.IsAny<CreateUpdatePantryItemRequestDto>(), userId, CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.AddItemAsync(new CreateUpdatePantryItemRequestDto { Id = 1, Quantity = 1, Food = new ExistingFoodReferenceDto { Id = 1 }}, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task AddItems_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.CreatePantryItemsAsync(It.IsAny<IEnumerable<CreateUpdatePantryItemRequestDto>>(), userId, CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.AddItemsAsync([new CreateUpdatePantryItemRequestDto{ Id = 1, Quantity = 1, Food = new ExistingFoodReferenceDto { Id = 1 }}], CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetItem_ReturnsNotFound_WhenNull()
        {
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1, CancellationToken.None)).ReturnsAsync((PantryItemDto)null!);

            var result = await _controller.GetItemAsync(1, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task GetItem_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.GetPantryItemByIdAsync(1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetItemAsync(1, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetItems_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.GetAllPantryItemsAsync(It.IsAny<int>(), 1, 10, CancellationToken.None)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GetItemsAsync(1, 10, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task DeleteItem_ReturnsNotFound_WhenFalse()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(It.IsAny<int>(), 1, CancellationToken.None)).ReturnsAsync(false);

            var result = await _controller.DeleteItemAsync(1, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteItem_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.DeletePantryItemAsync(It.IsAny<int>(), 1, CancellationToken.None)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.DeleteItemAsync(1, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task DeleteItems_ReturnsBadRequest_WhenRequestIsNull()
        {
            var result = await _controller.DeleteItemsAsync(null!, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task DeleteItems_ReturnsNotFound_WhenNoIdsDeleted()
        {
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), CancellationToken.None))
                .ReturnsAsync(new DeleteRequest { Ids = [] });

            var result = await _controller.DeleteItemsAsync(new DeleteRequest { Ids = [1] }, CancellationToken.None);
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task DeleteItems_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.DeletePantryItemsAsync(It.IsAny<int>(), It.IsAny<IEnumerable<int>>(), CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.DeleteItemsAsync(new DeleteRequest { Ids = [1] }, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task Search_ReturnsBadRequest_WhenQueryIsNullOrEmpty()
        {
            var result = await _controller.SearchAsync(null!, null, null, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task Search_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.Search(It.IsAny<string>(), userId, null, null, CancellationToken.None)).ThrowsAsync(new Exception("fail"));

            var result = await _controller.SearchAsync("test", null, null, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task Update_ReturnsBadRequest_WhenPantryItemIsNull()
        {
            var result = await _controller.UpdateAsync(1, null!, CancellationToken.None);
            var objResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal(400, objResult.StatusCode);
        }

        [Fact]
        public async Task Update_Returns500_OnException()
        {
            _serviceMock.Setup(s => s.UpdatePantryItemAsync(It.IsAny<CreateUpdatePantryItemRequestDto>(), userId, CancellationToken.None))
                .ThrowsAsync(new Exception("fail"));

            var result = await _controller.UpdateAsync(1, new CreateUpdatePantryItemRequestDto { Id = 1, Quantity = 1, Food = new ExistingFoodReferenceDto { Id = 1 }}, CancellationToken.None);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }
    }
}
