using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class FoodService(PlannerContext context, ILogger<FoodService> logger) : IFoodService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<FoodService> _logger = logger;

        public async Task<GetFoodsResult> SearchFoodsAsync(string search, int? skip, int? take)
        {
            _logger.LogInformation("Entering SearchFoodsAsync: search={Search}, skip={Skip}, take={Take}", search, skip, take);
            try
            {
                var foodsQuery = _context.Foods
                    .AsNoTracking()
                    .Include(f => f.Category)
                    .AsQueryable();

                if (!string.IsNullOrWhiteSpace(search))
                    foodsQuery = foodsQuery.Where(i => i.Name.Contains(search, StringComparison.InvariantCultureIgnoreCase));

                var count = await foodsQuery.CountAsync();

                if (skip != null)
                {
                    if (skip < 0)
                    {
                        _logger.LogError("SearchFoodsAsync: Negative skip {Skip}", skip);
                        throw new ArgumentException("skip must be non-negative");
                    }
                    foodsQuery = foodsQuery.Skip(skip.Value);
                }

                if (take != null)
                {
                    if (take < 0)
                    {
                        _logger.LogError("SearchFoodsAsync: Negative take {Take}", take);
                        throw new ArgumentException("take must be non-negative");
                    }
                    foodsQuery = foodsQuery.Take(take.Value);
                }

                var foods = await foodsQuery.OrderBy(i => i.Name).ToListAsync();
                _logger.LogInformation("SearchFoodsAsync: Found {Count} foods", foods.Count);
                return new GetFoodsResult { TotalCount = count, Items = foods.Select(i => i.ToDto()) };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchFoodsAsync: Failed to search foods");
                throw;
            }
            finally
            {
                _logger.LogInformation("Exiting SearchFoodsAsync: search={Search}", search);
            }
        }
    }
}