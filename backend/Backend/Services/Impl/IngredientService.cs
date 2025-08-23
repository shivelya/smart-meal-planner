using Backend.DTOs;
using Backend.Model;
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

        public async Task<IEnumerable<IngredientDto>> SearchIngredients(string search)
        {
            var ingredients = await _context.Ingredients
                .Where(i => i.Name.Contains(search))
                .OrderBy(i => i.Name)
                .Take(20) // limit results for performance
                .ToListAsync();

            return ingredients.Select(ToDto);
        }

        private IngredientDto ToDto(Ingredient source)
        {
            return new IngredientDto
            {
                Id = source.Id,
                Name = source.Name
            };
        }
    }
}