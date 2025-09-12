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
        /// <param name="query">The search term used to search foods by name.</param>
        /// <param name="skip">The number of results to skip for pagination.</param>
        /// <param name="take">The number of results to take for pagination.</param>
        /// <remarks>Returns a list of foods matching the given query, as well as the total count.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetFoodsResult>> SearchFoodsAsync([FromQuery, BindRequired] string query, int? skip = null, int? take = null)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                _logger.LogWarning("Search term is required.");
                return BadRequest("Search term is required.");
            }

            try
            {
                var foods = await _service.SearchFoodsAsync(query, skip, take);

                _logger.LogInformation("search on {search} completed with {count} results", query, foods.TotalCount);

                return Ok(foods);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not search for foods: {ex}", ex.Message);
                return StatusCode(500, "Could not search for foods: " + ex.Message);
            }
        }
    }
}