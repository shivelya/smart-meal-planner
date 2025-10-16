using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class FoodController(IFoodService service, ILogger<FoodController> logger) : PlannerControllerBase(logger)
    {
        private readonly IFoodService _service = service;
        private readonly ILogger<FoodController> _logger = logger;

        /// <summary>
        /// Search foods by name based on the search term provided.
        /// </summary>
        /// <param name="query">The search term used to search foods by name.</param>
        /// <param name="skip">The number of results to skip for pagination.</param>
        /// <param name="take">The number of results to take for pagination.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns a list of foods matching the given query, as well as the total count.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetFoodsResult>> SearchFoodsAsync([FromQuery] string? query, CancellationToken ct, int? skip = null, int? take = 50)
        {
            query = SanitizeInput(query);
            const string method = nameof(SearchFoodsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. query={Query}, skip={Skip}, take={Take}", method, nameof(FoodController), query, skip, take);

            return await TryCallToServiceAsync(method, async () =>
            {
                var foods = await _service.SearchFoodsAsync(query!, skip, take, ct);
                if (ResultNullCheck(method, foods) is { } check) return check;

                _logger.LogInformation("{Method}: Search on '{Query}' completed with {Count} results. Foods: {Foods}", method, query, foods.TotalCount, string.Join(",", foods.Items.Select(f => f.Name)));
                return Ok(foods);
            });
        }
    }
}