using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for managing pantry items.
    /// For authenticated user only.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="PantryItemController"/> class.
    /// </remarks>
    /// <param name="service">The pantry item service.</param>
    /// <param name="logger">The logger instance.</param>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PantryItemController(IPantryItemService service, ILogger<PantryItemController> logger) : PlannerControllerBase(logger)
    {
        private readonly IPantryItemService _service = service;
        private readonly ILogger<PantryItemController> _logger = logger;

        /// <summary>
        /// Adds a pantry item for the authenticated user. Assumes a FoodId in the Food property for foods that already exist,
        /// and assumes a FoodName and CategoryId in the Food property for foods that need to be added.
        /// </summary>
        /// <param name="dto">A DTO containing pantry item details.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Return a created pantry item DTO.</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> AddItemAsync(CreateUpdatePantryItemRequestDto dto, CancellationToken ct)
        {
            const string method = nameof(AddItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}", method, nameof(PantryItemController));

            if (CheckForNull(method, dto, nameof(dto)) is { } check2) return check2;
            if (CheckForLessThan0(method, dto.Quantity, nameof(dto.Quantity)) is { } check) return check;
            SanitizeRequest(dto);

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var result = await _service.CreatePantryItemAsync(dto, userId, ct);
                if (ResultNullCheck(method, result, userId.ToString()) is { } check) return check;

                _logger.LogInformation("{Method}: Pantry item added for user {UserId}. Id={Id}", method, userId, result.Id);
                return Created("", result);
            });
        }

        /// <summary>
        /// Adds multiple pantry items for the authenticated user. Assumes a FoodId in the Food property for foods that already exist,
        /// and assumes a FoodName and CategoryId in the Food property for foods that need to be added.
        /// </summary>
        /// <param name="dtos">A collection of DTOs containing pantry item details.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Return a collection of created pantry item DTOs.</remarks>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> AddItemsAsync(IEnumerable<CreateUpdatePantryItemRequestDto> dtos, CancellationToken ct)
        {
            const string method = nameof(AddItemsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}.", method, nameof(PantryItemController));
            if (CheckForNullOrEmpty(method, dtos, nameof(dtos)) is { } check2) return check2;

            foreach (var dto in dtos!)
            {
                if (dto == null) continue; // we skip null entries in the service so it's fine to skip them here

                SanitizeRequest(dto);
                if (CheckForLessThan0(method, dto.Quantity, nameof(dto.Quantity)) is { } check) return check;
            }

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var result = await _service.CreatePantryItemsAsync(dtos, userId, ct);
                if (ResultNullCheck(method, result, userId.ToString()) is { } check) return check;

                _logger.LogInformation("{Method}: Pantry items added for user {UserId}. TotalCount={Count}", method, userId, result.TotalCount);
                return Created("", result);
            });
        }

        /// <summary>
        /// Retrieves a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns the pantry item DTO if found, otherwise 404 Not Found.</remarks>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> GetItemAsync(int id, CancellationToken ct)
        {
            const string method = nameof(GetItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(PantryItemController), id);
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var item = await _service.GetPantryItemByIdAsync(id, ct);
                if (ResultNullCheck(method, item, id.ToString(), NotFound) is { } check) return check;

                _logger.LogInformation("{Method}: Pantry item retrieved. id={Id}", method, item!.Id);
                return Ok(item);
            });
        }

        /// <summary>
        /// Searches the current user's pantry items for the given name, with pagination.
        /// </summary>
        /// <param name="query">The search term to query on.</param>
        /// <param name="take">The number of responses to return for pagination.</param>
        /// <param name="skip">The number of responses to skip for pagination.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns the pantry items found, along with the total number of responses.</remarks>
        [HttpGet()]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> GetItemsAsync(string? query = null, int? take = null, int? skip = null, CancellationToken ct = default)
        {
            query = SanitizeInput(query);
            const string method = nameof(GetItemsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. query={Query}, take={Take}, skip={Skip}", method, nameof(PantryItemController), query, take, skip);
            if (CheckForLessThan0(method, skip, nameof(skip)) is { } check) return check;
            if (CheckForLessThanOrEqualTo0(method, take, nameof(take)) is { } check2) return check2;

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var results = await _service.GetPantryItemsAsync(userId, query, take, skip, ct);
                if (ResultNullCheck(method, results, userId.ToString()) is { } check) return check;

                _logger.LogInformation("{Method}: Retrieved {TotalCount} pantry items.", method, results.TotalCount);
                return Ok(results);
            });
        }

        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks> Returns No content if deleted, otherwise 404 Not Found.</remarks>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteItemAsync(int id, CancellationToken ct)
        {
            const string method = nameof(DeleteItemAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(PantryItemController), id);
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var deleted = await _service.DeletePantryItemAsync(userId, id, ct);
                if (ResultNullCheck<bool?>(method, deleted ? deleted : null, userId.ToString(), NotFound) is { } check) return check;

                _logger.LogInformation("{Method}: Pantry item with ID {Id} deleted.", method, id);
                return NoContent();
            });
        }

        /// <summary>
        /// Deletes multiple pantry items by their IDs.
        /// </summary>
        /// <param name="request">A collection of pantry item IDs to delete.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns Ok with the ids of deleted items, otherwise 404 if none are found.</remarks>
        [HttpPost("bulk-delete")]
        [ProducesResponseType(typeof(DeleteRequest), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeleteRequest>> DeleteItemsAsync([FromBody, BindRequired] DeleteRequest request, CancellationToken ct)
        {
            const string method = nameof(DeleteItemsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}", method, nameof(PantryItemController));
            if (CheckForNull(method, request, nameof(request)) is { } check2) return check2;
            if (CheckForNullOrEmpty(method, request.Ids, nameof(request.Ids)) is { } check3) return check3;

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var deleted = await _service.DeletePantryItemsAsync(userId, request.Ids, ct);
                if (ResultNullCheck(method, deleted.Ids.Any() ? deleted : null, userId.ToString(), NotFound) is { } check) return check;

                _logger.LogInformation("{Method}: Deleted {Count} pantry items.", method, deleted.Ids.Count());
                return StatusCode(204, deleted);
            });
        }

        /// <summary>
        /// Updates a given pantry item in the DB.
        /// </summary>
        /// <param name="id">The id of the pantryItem to update</param>
        /// <param name="pantryItem">The pantry item to update</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns the updated pantry item DTO, or 400 if an error occurs.</remarks>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> UpdateAsync(int id, [FromBody, BindRequired] CreateUpdatePantryItemRequestDto pantryItem, CancellationToken ct)
        {
            const string method = nameof(UpdateAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(PantryItemController), id);

            if (CheckForNull(method, pantryItem, nameof(pantryItem)) is { } check2) return check2;
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;
            if (CheckForLessThan0(method, pantryItem.Quantity, nameof(pantryItem.Quantity)) is { } check3) return check3;
            SanitizeRequest(pantryItem);

            var userId = GetUserId();
            return await TryCallToServiceAsync(method, async () =>
            {
                var updated = await _service.UpdatePantryItemAsync(pantryItem, userId, id, ct);
                if (ResultNullCheck(method, updated, userId.ToString()) is { } check) return check;

                _logger.LogInformation("{Method}: Pantry item updated successfully. id={Id}", method, updated.Id);
                return Ok(updated);
            });
        }

        private static void SanitizeRequest(CreateUpdatePantryItemRequestDto dto)
        {
            if (dto == null) return;

            dto.Unit = SanitizeInput(dto.Unit);
            if (dto.Food is NewFoodReferenceDto newOne)
                newOne.Name = SanitizeInput(newOne.Name);
        }
    }
}