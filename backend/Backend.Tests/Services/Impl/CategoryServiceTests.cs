using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class CategoryServiceTests
    {
        private readonly DbContextOptions<PlannerContext> _dbOptions;
        private readonly PlannerContext _context;
        private readonly Mock<ILogger<PlannerContext>> _loggerMock;
        private readonly CategoryService _service;

        public CategoryServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _loggerMock = new Mock<ILogger<PlannerContext>>();
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            _context = new PlannerContext(_dbOptions, config, _loggerMock.Object);
            _service = new CategoryService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllCategories()
        {
            _context.Categories.Add(new Category { Id = 1, Name = "Fruit" });
            _context.Categories.Add(new Category { Id = 2, Name = "Vegetable" });
            await _context.SaveChangesAsync();

            var result = await _service.GetAllAsync();
            Assert.Equal(2, result.TotalCount);
            Assert.Contains(result.Items, c => c.Name == "Fruit");
            Assert.Contains(result.Items, c => c.Name == "Vegetable");
        }

        [Fact]
        public async Task GetAllAsync_ReturnsEmpty_WhenNoCategories()
        {
            var result = await _service.GetAllAsync();
            Assert.Empty(result.Items);
        }
    }
}
