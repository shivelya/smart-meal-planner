using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryService _service;
        private readonly ILogger<CategoriesController> _logger;
        public CategoriesController(ICategoryService service, ILogger<CategoriesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetCategories()
        {
            var categories = await _service.GetAllAsync();
            _logger.LogInformation("Retrieved {Count} categories", categories.Count());
            return Ok(categories);
        }
    }
}