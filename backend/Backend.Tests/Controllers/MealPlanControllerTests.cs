using Moq;
using Microsoft.Extensions.Logging;
using Backend.Controllers;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Backend.Model;
using Backend.DTOs;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

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
            var mealPlans = new GetMealPlansResult { TotalCount = 1, MealPlans = [new MealPlanDto { Id = 1, Meals = [] }] };
            _mockService.Setup(s => s.GetMealPlansAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(mealPlans);

            var result = await _controller.GetMealPlansAsync();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(mealPlans, okResult.Value);
        }

        [Fact]
        public async Task GetMealPlans_ReturnsOk_WithEmptyList()
        {
            var mealPlan = new GetMealPlansResult { TotalCount = 0, MealPlans = [] };
            _mockService.Setup(s => s.GetMealPlansAsync(It.IsAny<int?>(), It.IsAny<int?>())).ReturnsAsync(mealPlan);

            var result = await _controller.GetMealPlansAsync();

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Empty(((GetMealPlansResult)okResult.Value!).MealPlans);
        }

        [Fact]
        public async Task CreateMealPlan_ReturnsCreated_WhenValid()
        {
            var date = DateTime.Now;
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [ new CreateUpdateMealPlanEntryRequestDto { Id = 1 } ], StartDate = date };
            var resultMealPlan = new MealPlanDto { Id = 1, Meals = [ new MealPlanEntryDto { Id = 1 } ], StartDate = date };
            _mockService.Setup(s => s.AddMealPlanAsync(It.IsAny<int>(), mealPlan)).ReturnsAsync(resultMealPlan);

            var result = await _controller.AddMealPlanAsync(mealPlan);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal(resultMealPlan, createdResult.Value);
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
            _mockService.Setup(s => s.UpdateMealPlanAsync(1, It.IsAny<int>(), mealPlan)).ReturnsAsync(resultMealPlan);

            var result = await _controller.UpdateMealPlanAsync(1, mealPlan);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(resultMealPlan, okResult.Value);
        }

        [Fact]
        public async Task UpdateMealPlan_Returns500_WhenNotFound()
        {
            var mealPlan = new CreateUpdateMealPlanRequestDto { Meals = [], Id = 1 };
            _mockService.Setup(s => s.UpdateMealPlanAsync(1, It.IsAny<int>(), mealPlan)).ThrowsAsync(new ArgumentException("message"));

            var result = await _controller.UpdateMealPlanAsync(1, mealPlan);

            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.Equal("Could not update meal plan: message", objResult.Value);
        }

        [Fact]
        public async Task UpdateMealPlan_ReturnsBadRequest_WhenNull()
        {
            var result = await _controller.UpdateMealPlanAsync(1, null!);
            Assert.IsType<BadRequestObjectResult>(result.Result);
        }

        [Fact]
        public async Task DeleteMealPlan_ReturnsNoContent_WhenSuccess()
        {
            _mockService.Setup(s => s.DeleteMealPlanAsync(1, It.IsAny<int>())).ReturnsAsync(true);
            var result = await _controller.DeleteMealPlanAsync(1);
            Assert.IsType<NoContentResult>(result);
        }

        [Fact]
        public async Task DeleteMealPlan_ReturnsNotFound_WhenNotFound()
        {
            _mockService.Setup(s => s.DeleteMealPlanAsync(1, It.IsAny<int>())).ReturnsAsync(false);
            var result = await _controller.DeleteMealPlanAsync(1);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ReturnsOk_WhenValid()
        {
            var generatedPlan = new GeneratedMealPlanDto {
                Meals = [
                    new GeneratedMealPlanEntryDto {
                        RecipeId = 1,
                        Recipe = new RecipeDto
                        {
                            Id = 1,
                            Title = "Test Recipe",
                            UserId = 1,
                            Source = "src",
                            Instructions = "inst",
                            Ingredients = [] }
                    }
                ]
            };
            _mockService.Setup(s => s.GenerateMealPlanAsync(5, It.IsAny<int>(), It.IsAny<DateTime>())).ReturnsAsync(generatedPlan);

            var result = await _controller.GenerateMealPlanAsync(5, DateTime.Today);

            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Equal(generatedPlan, okResult.Value);
        }

        [Fact]
        public async Task GenerateMealPlanAsync_ReturnsBadRequest_WhenDaysExceedMax()
        {
            var result = await _controller.GenerateMealPlanAsync(15, DateTime.Today); // MAXDAYS is 10 in test config

            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.NotNull(badRequest.Value);
            Assert.Contains("Cannot create meal plan for more than", badRequest.Value!.ToString());
        }

        [Fact]
        public async Task GenerateMealPlanAsync_Returns500_OnException()
        {
            _mockService.Setup(s => s.GenerateMealPlanAsync(5, It.IsAny<int>(), It.IsAny<DateTime>())).ThrowsAsync(new Exception("fail"));

            var result = await _controller.GenerateMealPlanAsync(5, DateTime.Today);

            var objResult = Assert.IsType<ObjectResult>(result.Result);
            Assert.Equal(500, objResult.StatusCode);
            Assert.NotNull(objResult.Value);
            Assert.Contains("Could not generate meal plan", objResult.Value!.ToString());
        }
    }
}
