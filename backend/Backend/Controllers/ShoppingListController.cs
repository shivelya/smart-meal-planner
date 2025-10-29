using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ShoppingListController(IShoppingListService service, ILogger<ShoppingListController> logger) : PlannerControllerBase(logger)
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
            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Getting shopping list for userId={UserId}", method, userId);
                var result = await _service.GetShoppingListAsync(userId, ct);
                if (ResultNullCheck(method, result) is { } check) return check;

                _logger.LogInformation("{Method}: Retrieved shopping list for userId={UserId}", method, userId);
                return Ok(result);
            });
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
            _logger.LogInformation("{Method}: Entering", method);
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
            if (CheckForLessThanOrEqualTo0(method, request.Id, nameof(request.Id)) is { } check2) return check2;

            request.Notes = SanitizeInput(request.Notes);

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Updating shopping list item for userId={UserId}", method, userId);
                var result = await _service.UpdateShoppingListItemAsync(request, userId, ct);
                if (ResultNullCheck(method, result) is { } check3) return check3;

                _logger.LogInformation("{Method}: Updated shopping list item for userId={UserId}", method, userId);
                return Ok(result);
            });
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
            _logger.LogInformation("{Method}: Entering", method);
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
            if (CheckForLessThanOrEqualTo0(method, request.Id, nameof(request.Id)) is { } check2) return check2;

            request.Notes = SanitizeInput(request.Notes);

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Adding shopping list item for userId={UserId}", method, userId);
                var result = await _service.AddShoppingListItemAsync(request, userId, ct);
                if (ResultNullCheck(method, result) is { } check3) return check3;

                _logger.LogInformation("{Method}: Added shopping list item for userId={UserId}", method, userId);
                return Ok(result);
            });
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
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Deleting shopping list item for userId={UserId}, id={Id}", method, userId, id);
                var ok = await _service.DeleteShoppingListItemAsync(id, userId, ct);
                if (ResultNullCheck(method, ok ? "" : null, ret: NotFound) is { } check3) return check3;

                _logger.LogInformation("{Method}: Item deleted. id={Id}", method, id);
                return NoContent();
            });
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
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, request.MealPlanId, nameof(request.MealPlanId)) is { } check2) return check2;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                _logger.LogInformation("{Method}: Generating shopping list for userId={UserId}, mealPlanId={MealPlanId}, restart={Restart}", method, userId, request.MealPlanId, request.Restart);
                await _service.GenerateAsync(request, userId, ct);

                _logger.LogInformation("{Method}: Generated shopping list for userId={UserId}", method, userId);
                return Ok();
            });
        }
    }
}