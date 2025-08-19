using Microsoft.EntityFrameworkCore;
using Backend;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Services.Impl
{
    public class CategoryService : ICategoryService
    {
        private readonly PlannerContext _context;
        public CategoryService(PlannerContext context) => _context = context;

        public async Task<IEnumerable<CategoryDto>> GetAllAsync()
        {
            return await _context.Categories
                .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }
    }
}