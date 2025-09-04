using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class FoodService : IFoodService
    {
        private readonly PlannerContext _context;
        private readonly ILogger<FoodService> _logger;
        public FoodService(PlannerContext context, ILogger<FoodService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FoodReferenceDto>> SearchFoods(string search)
        {
            var foods = await _context.Foods
                .Where(i => i.Name.Contains(search))
                .OrderBy(i => i.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return foods.Select(i => i.ToDto());
        }
    }
}