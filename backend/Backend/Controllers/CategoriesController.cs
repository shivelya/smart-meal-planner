using Microsoft.AspNetCore.Mvc;
using SmartMealPlannerBackend.DTOs;
using SmartMealPlannerBackend.Services;

namespace SmartMealPlannerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;
        public CategoriesController(ICategoryService service) => _service = service;

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _service.GetAllAsync();
            return Ok(categories);
        }
    }
}