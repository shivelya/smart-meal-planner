using Moq;
using Microsoft.Extensions.Logging;
using Backend.Controllers;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Backend.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Security;

namespace Backend.Tests.Controllers
{
    public class MealPlanControllerTests
    {
        private readonly Mock<IMealPlanService> _mockService;
        private readonly Mock<ILogger<MealPlanController>> _mockLogger;
        private readonly MealPlanController _controller;
        private readonly int userId;

        public MealPlanControllerTests()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["MaxMealPlanGenerationDays"] = "10",
            };

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _mockService = new Mock<IMealPlanService>();
            _mockLogger = new Mock<ILogger<MealPlanController>>();
            _controller = new MealPlanController(_mockService.Object, _mockLogger.Object, config);

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
        public async Task GetMealPlans_ReturnsOk_WithMealPlans()
        {
            var mealPlans = new GetMealPlansResult { TotalCount = 1, Items = [new MealPlanDto { Id = 1, Meals = [] }] };
            _mockService.Setup(s => s.GetMealPlansAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), CancellationToken.None)).ReturnsAsync(mealPlans);

            var result = await _controller.GetMealPlansAsync();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(mealPlans, okResult.Value);
        }

        [Fact]
        public async Task GetMealPlans_Returns500_WhenServiceReturnsNull()
        {
            _mockService.Setup(s => s.GetMealPlansAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), CancellationToken.None)).ReturnsAsync((GetMealPlansResult)null!);
            var result = await _controller.GetMealPlansAsync();
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GetMealPlans_ReturnsOk_WithEmptyList()
        {
            var mealPlan = new GetMealPlansResult { TotalCount = 0, Items = [] };
            _mockService.Setup(s => s.GetMealPlansAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), CancellationToken.None)).ReturnsAsync(mealPlan);

            var result = await _controller.GetMealPlansAsync();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(((GetMealPlansResult)okResult.Value!).Items);
        }

        [Fact]
    public async Task GetMealPlans_ReturnsBadRequest_WhenSkipIsNegative()
    {
        var result = await _controller.GetMealPlansAsync(skip: -1);
        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Skip must be greater than or equal to zero.", badRequest.Value);
    }

    [Fact]
    public async Task GetMealPlans_ReturnsBadRequest_WhenTakeIsZeroOrNegative()
    {
        var resultZero = await _controller.GetMealPlansAsync(take: 0);
        var badRequestZero = Assert.IsType<BadRequestObjectResult>(resultZero.Result);
        Assert.Equal("Take must be greater than zero.", badRequestZero.Value);

        var resultNegative = await _controller.GetMealPlansAsync(take: -5);
        var badRequestNegative = Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
        Assert.Equal("Take must be greater than zero.", badRequestNegative.Value);
    }

        [Fact]
        public async Task CreateMealPlan_ReturnsCreated_WhenValid()
        {
            var date = DateTime.Now;
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto { Id = 1 }], StartDate = date };
            var resultMealPlan = new MealPlanDto { Id = 1, Meals = [new MealPlanEntryDto { Id = 1 }], StartDate = date };
            _mockService.Setup(s => s.AddMealPlanAsync(It.IsAny<int>(), mealPlan, CancellationToken.None)).ReturnsAsync(resultMealPlan);

            var result = await _controller.AddMealPlanAsync(mealPlan);

            var createdResult = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(resultMealPlan, createdResult.Value);
        }

        [Fact]
        public async Task CreateMealPlan_Returns500_WhenServiceReturnsNull()
        {
            var date = DateTime.Now;
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [new CreateUpdateMealPlanEntryRequestDto { Id = 1 }], StartDate = date };
            _mockService.Setup(s => s.AddMealPlanAsync(It.IsAny<int>(), mealPlan, CancellationToken.None)).ReturnsAsync((MealPlanDto)null!);
            var result = await _controller.AddMealPlanAsync(mealPlan);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task CreateMealPlan_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.AddMealPlanAsync(null!);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateMealPlan_ReturnsObject_WhenSuccess()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [], Id = 1 };
            var resultMealPlan = new MealPlanDto { Id = 1, Meals = [] };
            _mockService.Setup(s => s.UpdateMealPlanAsync(1, It.IsAny<int>(), mealPlan, CancellationToken.None)).ReturnsAsync(resultMealPlan);

            var result = await _controller.UpdateMealPlanAsync(1, mealPlan);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultMealPlan, okResult.Value);
        }

        [Fact]
        public async Task UpdateMealPlan_Returns500_WhenServiceReturnsNull()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [], Id = 1 };
            _mockService.Setup(s => s.UpdateMealPlanAsync(1, It.IsAny<int>(), mealPlan, CancellationToken.None)).ReturnsAsync((MealPlanDto)null!);
            var result = await _controller.UpdateMealPlanAsync(1, mealPlan);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task UpdateMealPlan_Returns500_WhenNotFound()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [], Id = 1 };
            _mockService.Setup(s => s.UpdateMealPlanAsync(1, It.IsAny<int>(), mealPlan, CancellationToken.None)).ThrowsAsync(new ArgumentException("message"));

            var result = await _controller.UpdateMealPlanAsync(1, mealPlan);

            var objResult = Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UpdateMealPlan_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.UpdateMealPlanAsync(1, null!);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task UpdateMealPlan_ReturnsBadRequest_WhenIdIsZeroOrNegative()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [], Id = 1 };
            var resultZero = await _controller.UpdateMealPlanAsync(0, mealPlan);
            var badRequestZero = Assert.IsType<BadRequestObjectResult>(resultZero.Result);
            Assert.Equal("Id must be positive.", badRequestZero.Value);

            var resultNegative = await _controller.UpdateMealPlanAsync(-5, mealPlan);
            var badRequestNegative = Assert.IsType<BadRequestObjectResult>(resultNegative.Result);
            Assert.Equal("Id must be positive.", badRequestNegative.Value);
        }

        [Fact]
        public async Task DeleteMealPlan_ReturnsNoContent_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteMealPlanAsync(1, It.IsAny<int>(), CancellationToken.None)).ReturnsAsync(true);
            var result = await _controller.DeleteMealPlanAsync(1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteMealPlan_ReturnsNotFound_WhenNotFound()
        {
            _mockService.Setup(s => s.DeleteMealPlanAsync(1, It.IsAny<int>(), CancellationToken.None)).Throws<SecurityException>();
            var result = await _controller.DeleteMealPlanAsync(1);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DeleteMealPlan_ReturnsBadRequest_WhenIdIsZeroOrNegative()
        {
            var resultZero = await _controller.DeleteMealPlanAsync(0);
            var badRequestZero = Assert.IsType<BadRequestObjectResult>(resultZero);
            Assert.Equal("Id must be positive.", badRequestZero.Value);

            var resultNegative = await _controller.DeleteMealPlanAsync(-5);
            var badRequestNegative = Assert.IsType<BadRequestObjectResult>(resultNegative);
            Assert.Equal("Id must be positive.", badRequestNegative.Value);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ReturnsOk_WhenValid()
        {
            var generatedPlan = new CreateUpdateMealPlanRequestDto
            {
                Meals = [ new CreateUpdateMealPlanEntryRequestDto { RecipeId = 1 } ]
            };

            var request = new GenerateMealPlanRequestDto
            {
                Days = 5,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            _mockService.Setup(s => s.GenerateMealPlanAsync(request, It.IsAny<int>(), CancellationToken.None)).ReturnsAsync(generatedPlan);
            var result = await _controller.GenerateMealPlanAsync(request);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(generatedPlan, okResult.Value);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_Returns500_WhenServiceReturnsNull()
        {
            var request = new GenerateMealPlanRequestDto
            {
                Days = 5,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            _mockService.Setup(s => s.GenerateMealPlanAsync(request, It.IsAny<int>(), CancellationToken.None)).ReturnsAsync((CreateUpdateMealPlanRequestDto)null!);
            var result = await _controller.GenerateMealPlanAsync(request);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ReturnsBadRequest_WhenDaysExceedMax()
        {
            var request = new GenerateMealPlanRequestDto
            {
                Days = 15,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            var result = await _controller.GenerateMealPlanAsync(request); // MAXDAYS is 10 in test config

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Cannot create meal plan for more than", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task GenerateMealPlanAsync_Returns500_OnException()
        {
            var request = new GenerateMealPlanRequestDto
            {
                Days = 5,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            _mockService.Setup(s => s.GenerateMealPlanAsync(request, It.IsAny<int>(), CancellationToken.None)).ThrowsAsync(new Exception("fail"));
            var result = await _controller.GenerateMealPlanAsync(request);

            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_Returns503_OnExternalFailure()
        {
            var request = new GenerateMealPlanRequestDto
            {
                Days = 5,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            _mockService.Setup(s => s.GenerateMealPlanAsync(request, It.IsAny<int>(), CancellationToken.None)).ThrowsAsync(new HttpRequestException("fail"));
            var result = await _controller.GenerateMealPlanAsync(request);

            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(503, objResult.StatusCode);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(-10)]
        public async Task GenerateMealPlanAsync_ReturnsBadRequest_WhenDaysIsZeroOrNegative(int days)
        {
            var request = new GenerateMealPlanRequestDto
            {
                Days = days,
                StartDate = DateTime.Today,
                UseExternal = false
            };
            var result = await _controller.GenerateMealPlanAsync(request);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task CookMeal_ReturnsBadRequest_WhenIdIsNonPositive()
        {
            var result = await _controller.CookMealAsync(0, 1);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result!);
            Assert.Equal("Id must be positive.", badRequest.Value);
        }

        [Fact]
        public async Task CookMeal_ReturnsBadRequest_WhenMealEntryIdIsNonPositive()
        {
            var result = await _controller.CookMealAsync(1, 0);
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result!);
            Assert.Equal("mealEntryId must be positive.", badRequest.Value);
        }

        [Fact]
        public async Task CookMeal_ReturnsOk_WhenServiceReturnsResult()
        {
            var pantryResult = new GetPantryItemsResult { TotalCount = 1, Items = [] };
            _mockService.Setup(s => s.CookMealAsync(1, 2, userId, CancellationToken.None)).ReturnsAsync(pantryResult);

            var result = await _controller.CookMealAsync(1, 2);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(pantryResult, okResult.Value);
        }

        [Fact]
        public async Task CookMeal_Returns500_WhenServiceReturnsNull()
        {
            _mockService.Setup(s => s.CookMealAsync(1, 2, userId, CancellationToken.None)).ReturnsAsync((GetPantryItemsResult)null!);
            var result = await _controller.CookMealAsync(1, 2);
            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }

        [Fact]
        public async Task CookMeal_Returns500_WhenServiceThrows()
        {
            _mockService.Setup(s => s.CookMealAsync(1, 2, userId, CancellationToken.None)).Throws(new Exception("fail"));

            var result = await _controller.CookMealAsync(1, 2);

            var objResult = Assert.IsType<StatusCodeResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
        }
    }
}
