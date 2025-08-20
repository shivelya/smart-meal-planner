using Microsoft.EntityFrameworkCore;
using Backend.DTOs;

namespace Backend.Services.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly PlannerContext _context;
        private readonly ILogger<PlannerContext> _logger;
        public CategoryService(PlannerContext context, ILogger<PlannerContext> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} categories", categories.Count);
            return categories;
        }
    }
}