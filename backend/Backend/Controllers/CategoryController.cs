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
        /// <param name="ct">Cancellation token, unsenn by the user.</param>
        /// <remarks>Returns all the existing category types.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetCategoriesResult>> GetCategories(CancellationToken ct)
        {
            const string method = nameof(GetCategories);
            _logger.LogInformation("{Method}: Entering {Controller}. No input parameters.", method, nameof(CategoriesController));
            try
            {
                var categories = await _service.GetAllAsync(ct);
                if (categories == null)
                {
                    _logger.LogWarning("{Method}: Service returned null categories.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null categories.");
                }

                _logger.LogInformation("{Method}: Retrieved {Count} categories. Categories: {Categories}", method, categories.TotalCount, string.Join(",", categories.Items.Select(c => c.Name)));
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, "Could not retrieve categories: " + ex.Message);
            }
        }
    }
}