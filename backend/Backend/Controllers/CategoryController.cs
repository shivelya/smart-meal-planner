using Microsoft.AspNetCore.Mvc;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    /// <summary>
    /// Lists all category types. They are currently static.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController(ICategoryService service, ILogger<CategoriesController> logger) : ControllerBase
    {
        private readonly ICategoryService _service = service;
        private readonly ILogger<CategoriesController> _logger = logger;

        /// <summary>
        /// Lists all category types. They are currently static.
        /// </summary>
        /// <remarks>Returns all the existing category types.</remarks>
        [HttpGet]
        public async Task<ActionResult<GetCategoriesResult>> GetCategories()
        {
            try
            {
                var categories = await _service.GetAllAsync();
                _logger.LogInformation("Retrieved {Count} categories", categories.Count());
                return Ok(new GetCategoriesResult { TotalCount = categories.Count(), Items = categories });
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Could not retrieve categories: " + ex.Message);
            }
        }
    }
}