using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class FoodService(PlannerContext context, ILogger<FoodService> logger) : IFoodService
    {
        private readonly PlannerContext _context = context;
        private readonly ILogger<FoodService> _logger = logger;

        public async Task<GetFoodsResult> SearchFoods(string search, int? skip, int? take)
        {
            var foodsQuery = _context.Foods
                .Where(i => i.Name.Contains(search));

            var count = await foodsQuery.CountAsync();

            if (skip != null)
            {
                if (skip < 0)
                {
                    _logger.LogError("Negative skip used for search.");
                    throw new ArgumentException("skip must be non-negative");
                }

                foodsQuery = foodsQuery.Skip(skip.Value);
            }

            if (take != null)
            {
                if (take < 0)
                {
                    _logger.LogError("Negative take used for search.");
                    throw new ArgumentException("take must be non-negative");
                }

                foodsQuery = foodsQuery.Take(take.Value);
            }

            var foods = await foodsQuery.OrderBy(i => i.Name).ToListAsync();

            return new GetFoodsResult { TotalCount = count, Items = foods.Select(i => i.ToDto()) };
        }
    }
}