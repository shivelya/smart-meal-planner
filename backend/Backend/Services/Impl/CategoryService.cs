using Microsoft.EntityFrameworkCore;
using Backend.DTOs;

namespace Backend.Services.Impl
{
    public class CategoryService(PlannerContext context, ILogger<PlannerContext> logger) : ICategoryService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<PlannerContext> _logger = logger;

        /// <summary>
        /// Retrieves all categories from the database.
        /// </summary>
        /// <returns>An enumerable collection of category DTOs.</returns>
        public async Task<GetCategoriesResult> GetAllAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Select(c => c.ToDto())
                .ToListAsync();

            _logger.LogInformation("Retrieved {Count} categories", categories.Count);
            return new GetCategoriesResult { TotalCount = categories.Count, Items = categories };
        }
    }
}