using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoodController(IFoodService service, ILogger<FoodController> logger) : ControllerBase
    {
        private readonly IFoodService _service = service;
        private readonly ILogger<FoodController> _logger = logger;

        /// <summary>
        /// Search foods by name based on the search term provided.
        /// </summary>
        /// <param name="search">The search term used to search foods by name.</param>
        /// <remarks>Returns a list of foods matching the given query.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetFoodsResult>> SearchFoods([FromQuery, BindRequired] string search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                _logger.LogWarning("Search term is required.");
                return BadRequest("Search term is required.");
            }

            try
            {
                var foods = await _service.SearchFoods(search);

                _logger.LogInformation("search on {search} completed with {count} results", search, foods.Count());

                return Ok(new GetFoodsResult { TotalCount = foods.Count(), Items = foods });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not search for ingredients: {ex}", ex.Message);
                return StatusCode(500, "Could not search for ingredients: " + ex.Message);
            }
        }
    }
}