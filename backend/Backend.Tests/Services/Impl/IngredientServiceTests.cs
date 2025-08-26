using Backend.Model;
using Backend.Services.Impl;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.Services.Impl
{
    public class IngredientServiceTests
    {
        private readonly DbContextOptions<PlannerContext> _dbOptions;
        private readonly PlannerContext _context;
        private readonly Mock<ILogger<IngredientService>> _loggerMock;
        private readonly IngredientService _service;

        public IngredientServiceTests()
        {
            _dbOptions = new DbContextOptionsBuilder<PlannerContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _loggerMock = new Mock<ILogger<IngredientService>>();
            var configDict = new Dictionary<string, string?>();
            var config = new ConfigurationBuilder().AddInMemoryCollection(configDict).Build();
            _context = new PlannerContext(_dbOptions, config, new Mock<ILogger<PlannerContext>>().Object);
            _service = new IngredientService(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task SearchIngredients_ReturnsMatchingIngredients()
        {
            _context.Ingredients.Add(new Ingredient { Id = 1, Name = "Salt", CategoryId = 1, Category = new Category { Id = 1, Name = "Spices" } });
            _context.Ingredients.Add(new Ingredient { Id = 2, Name = "Pepper", CategoryId = 1, Category = new Category { Id = 3, Name = "Spices" } });
            _context.Ingredients.Add(new Ingredient { Id = 3, Name = "Sugar", CategoryId = 2, Category = new Category { Id = 2, Name = "Sweeteners" } });
            await _context.SaveChangesAsync();

            var result = await _service.SearchIngredients("S");
            Assert.Contains(result, i => i.Name == "Salt");
            Assert.Contains(result, i => i.Name == "Sugar");
            Assert.DoesNotContain(result, i => i.Name == "Pepper");
        }

        [Fact]
        public async Task SearchIngredients_ReturnsEmpty_WhenNoMatch()
        {
            var result = await _service.SearchIngredients("xyz");
            Assert.Empty(result);
        }

        [Fact]
        public async Task SearchIngredients_LimitsResultsTo20()
        {
            for (int i = 0; i < 25; i++)
            {
                _context.Ingredients.Add(new Ingredient { Id = i + 1, Name = $"Ing{i}", CategoryId = i+1,
                    Category = new Category { Id = i+1, Name = "Cat" } });
            }
            await _context.SaveChangesAsync();
            var result = await _service.SearchIngredients("Ing");
            Assert.Equal(20, result.Count());
        }
    }
}
