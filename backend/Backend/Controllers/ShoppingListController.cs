using System.Globalization;
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingListController(IShoppingListService service, ILogger<ShoppingListController> logger) : ControllerBase
    {
        private readonly IShoppingListService _service = service;
        private readonly ILogger<ShoppingListController> _logger = logger;

        /// <summary>
        /// Retrieves the shopping list for the current user 
        /// </summary>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns the shopping list.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetShoppingListResult>> GetShoppingListAsync(CancellationToken ct = default)
        {
            const string method = nameof(GetShoppingListAsync);
            _logger.LogInformation("{Method}: Entering {Controller}", method, nameof(ShoppingListController));
            try
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Getting shopping list for userId={UserId}", method, userId);
                var result = await _service.GetShoppingListAsync(userId, ct);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Edits an item on the shopping list
        /// </summary>
        /// <param name="request">The updated shopping list item.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns the shopping list.</remarks>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShoppingListItemDto>> UpdateShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(UpdateShoppingListItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. request={Request}", method, nameof(ShoppingListController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("request object is required.");
            }

            try
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Updating shopping list item for userId={UserId}", method, userId);
                var result = await _service.UpdateShoppingListItemAsync(request, userId, ct);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Adds a new item to the shopping list
        /// </summary>
        /// <param name="request">The new shopping list item.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns the new shopping list item.</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShoppingListItemDto>> AddShoppingListItemAsync(CreateUpdateShoppingListEntryRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(AddShoppingListItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. request={Request}", method, nameof(ShoppingListController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("request object is required.");
            }

            try
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Adding shopping list item for userId={UserId}", method, userId);
                var result = await _service.AddShoppingListItemAsync(request, userId, ct);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Removes an item from the shopping list
        /// </summary>
        /// <param name="id">The id of the item to remove.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns 204 on success.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ShoppingListItemDto>> DeleteShoppingListItemAsync(int id, CancellationToken ct = default)
        {
            const string method = nameof(DeleteShoppingListItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(ShoppingListController), id);
            if (id <= 0)
            {
                _logger.LogWarning("{Method}: Valid id is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id={Id}", method, id);
                return BadRequest("Valid id is required.");
            }

            try
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Deleting shopping list item for userId={UserId}, id={Id}", method, userId, id);
                var ok = await _service.DeleteShoppingListItemAsync(id, userId, ct);

                if (ok)
                {
                    _logger.LogInformation("{Method}: Item deleted. id={Id}", method, id);
                    _logger.LogInformation("{Method}: Exiting successfully.", method);
                    return NoContent();
                }
                else
                {
                    _logger.LogWarning("{Method}: Item not found. id={Id}", method, id);
                    _logger.LogInformation("{Method}: Exiting with NotFound. id={Id}", method, id);
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Generates a shopping list based on a given meal plan. If "restart" is true, any existing shopping list items will be cleared first.
        /// This DOES save the shopping list to the database. It does not return the shopping list, just a 200 status if successful.
        /// </summary>
        /// <param name="request">Includes a meal plan id and whether or not to restart the list.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Ok on success. Use GET api/shoppingList/ to retrieve the list.</remarks>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetShoppingListResult>> GenerateAsync(GenerateShoppingListRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(GenerateAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. request={Request}", method, nameof(ShoppingListController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("request object is required.");
            }

            if (request.MealPlanId <= 0)
            {
                _logger.LogWarning("{Method}: Valid meal plan id is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. mealPlanId={MealPlanId}", method, request.MealPlanId);
                return BadRequest("Valid meal plan id is required.");
            }

            try
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Generating shopping list for userId={UserId}, mealPlanId={MealPlanId}, restart={Restart}", method, userId, request.MealPlanId, request.Restart);
                await _service.GenerateAsync(request, userId, ct);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!, CultureInfo.InvariantCulture);
        }
    }
}