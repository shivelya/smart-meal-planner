using Backend.DTOs;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Impl
{
    public class IngredientService : IIngredientService
    {
        private readonly PlannerContext _context;
        private readonly ILogger<IngredientService> _logger;
        public IngredientService(PlannerContext context, ILogger<IngredientService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<FoodReferenceDto>> SearchIngredients(string search)
        {
            var ingredients = await _context.Foods
                .Where(i => i.Name.Contains(search))
                .OrderBy(i => i.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return ingredients.Select(i => i.ToDto());
        }
    }
}