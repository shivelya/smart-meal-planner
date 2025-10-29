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
    public class CategoryController(ICategoryService service, ILogger<CategoryController> logger) : PlannerControllerBase(logger)
    {
        private readonly ICategoryService _service = service;
        private readonly ILogger<CategoryController> _logger = logger;

        /// <summary>
        /// Lists all category types. They are currently static.
        /// </summary>
        /// <param name="ct">Cancellation token, unseen by the user.</param>
        /// <remarks>Returns all the existing category types.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetCategoriesResult>> GetCategories(CancellationToken ct)
        {
            const string method = nameof(GetCategories);
            _logger.LogInformation("{Method}: Entering {Controller}. No input parameters.", method, nameof(CategoryController));

            return await TryCallToServiceAsync(method, async () =>
            {
                var categories = await _service.GetAllAsync(ct);
                if (ResultNullCheck(method, categories) is { } check) return check;

                _logger.LogInformation("{Method}: Retrieved {Count} categories. Categories: {Categories}", method, categories.TotalCount, string.Join(",", categories.Items.Select(c => c.Name)));
                return Ok(categories);
            });
        }
    }
}