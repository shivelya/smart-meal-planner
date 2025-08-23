using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class IngredientController : ControllerBase
    {
        private readonly IIngredientService _service;
        private readonly ILogger<IngredientController> _logger;
        public IngredientController(IIngredientService service, ILogger<IngredientController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<IngredientDto>>> SearchIngredients([FromQuery] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                _logger.LogWarning("Search term is required.");
                return BadRequest("Search term is required.");
            }

            var ingredients = await _service.SearchIngredients(search);

            _logger.LogInformation("search on {0} completed with {1} results", search, ingredients.Count());

            return Ok(ingredients);
        }
    }
}