using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class FoodServiceTests
    {
        private readonly DbContextOptions<PlannerContext> _dbOptions;
        private readonly PlannerContext _context;
        private readonly Mock<ILogger<FoodService>> _loggerMock;
        private readonly FoodService _service;

        public FoodServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _loggerMock = new Mock<ILogger<FoodService>>();
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            _context = new PlannerContext(_dbOptions, config, new Mock<ILogger<PlannerContext>>().Object);
            _service = new FoodService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task SearchsFoods_ReturnsMatchingFoods()
        {
            _context.Foods.Add(new Food { Id = 1, Name = "Salt", CategoryId = 1, Category = new Category { Id = 1, Name = "Spices" } });
            _context.Foods.Add(new Food { Id = 2, Name = "Pepper", CategoryId = 3, Category = new Category { Id = 3, Name = "Spices" } });
            _context.Foods.Add(new Food { Id = 3, Name = "Sugar", CategoryId = 2, Category = new Category { Id = 2, Name = "Sweeteners" } });
            await _context.SaveChangesAsync();

            var result = await _service.SearchFoodsAsync("S", null, null, CancellationToken.None);
            Assert.Contains(result.Items, i => i.Name == "Salt");
            Assert.Contains(result.Items, i => i.Name == "Sugar");
            Assert.DoesNotContain(result.Items, i => i.Name == "Pepper");

            Assert.Equal(2, result.TotalCount);
            Assert.Equal(2, result.Items.Count());
            Assert.Equal("Spices", result.Items.First(i => i.Name == "Salt").Category.Name);
            Assert.Equal("Sweeteners", result.Items.First(i => i.Name == "Sugar").Category.Name);
        }

        [Fact]
        public async Task SearchFoods_ReturnsEmpty_WhenNoMatch()
        {
            var result = await _service.SearchFoodsAsync("xyz", null, null, CancellationToken.None);
            Assert.Empty(result.Items);
        }

        [Fact]
        public async Task SearchFoods_LimitsResultsTo20()
        {
            for (int i = 0; i < 25; i++)
            {
                _context.Foods.Add(new Food { Id = i + 1, Name = $"Ing{i}", CategoryId = i+1,
                    Category = new Category { Id = i+1, Name = "Cat" } });
            }
            await _context.SaveChangesAsync();
            var result = await _service.SearchFoodsAsync("Ing", 0, 20, CancellationToken.None);
            Assert.Equal(25, result.TotalCount);
            Assert.Equal(20, result.Items.Count());
        }
    }
}
