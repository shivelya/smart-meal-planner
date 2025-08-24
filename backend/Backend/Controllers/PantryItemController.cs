using System.Globalization;
using System.Security.Claims;
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
    public class PantryItemController(IPantryItemService service, ILogger<PantryItemController> logger) : ControllerBase
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
            _logger.LogInformation("{Method}: Entering {Controller}. dto={Dto}", method, nameof(PantryItemController), dto);
            if (dto == null)
            {
                _logger.LogWarning("{Method}: PantryItemDto dto is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. dto=null", method);
                return BadRequest("PantryItemDto dto is required.");
            }

            var userId = GetUserId();
            try
            {
                var result = await _service.CreatePantryItemAsync(dto, userId, ct);
                if (result == null)
                {
                    _logger.LogWarning("{Method}: Service returned null pantry item.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null pantry item.");
                }

                _logger.LogInformation("{Method}: Pantry item added for user {UserId}. Id={Id}", method, userId, result.Id);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Created("", result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
            _logger.LogInformation("{Method}: Entering {Controller}. dtos={Dtos}", method, nameof(PantryItemController), dtos);
            if (dtos == null || !dtos.Any())
            {
                _logger.LogWarning("{Method}: A collection of PantryItemDto dtos is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. dtos=null or empty", method);
                return BadRequest("A collection of PantryItemDto dtos is required.");
            }

            var userId = GetUserId();
            try
            {
                var result = await _service.CreatePantryItemsAsync(dtos, userId, ct);
                if (result == null)
                {
                    _logger.LogWarning("{Method}: Service returned null pantry items.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null pantry items.");
                }

                _logger.LogInformation("{Method}: Pantry items added for user {UserId}. TotalCount={Count}", method, userId, result.TotalCount);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return CreatedAtAction(nameof(GetItemAsync), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
            if (id <= 0)
            {
                _logger.LogWarning("{Method}: A valid pantry item ID is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id={Id}", method, id);
                return BadRequest("A valid pantry item ID is required.");
            }

            try
            {
                var item = await _service.GetPantryItemByIdAsync(id, ct);
                if (item is null)
                {
                    _logger.LogWarning("{Method}: Pantry item with ID {Id} not found.", method, id);
                    _logger.LogInformation("{Method}: Exiting with NotFound. id={Id}", method, id);
                    return NotFound();
                }

                _logger.LogInformation("{Method}: Pantry item retrieved. id={Id}", method, item.Id);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Retrieves all pantry items for the authenticated user with pagination.
        /// </summary>
        /// <param name="skip">The page number to retrieve.</param>
        /// <param name="take">The number of items per page.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Return a GetPantryItemsResult object containing the total count and fully constituted items.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> GetItemsAsync([FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            const string method = nameof(GetItemsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. skip={Skip}, take={Take}", method, nameof(PantryItemController), skip, take);
            try
            {
                var result = await _service.GetAllPantryItemsAsync(skip, take, ct);
                if (result == null)
                {
                    _logger.LogWarning("{Method}: Service returned null pantry items.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null pantry items.");
                }

                _logger.LogInformation("{Method}: Retrieved {TotalCount} pantry items.", method, result.TotalCount);
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
            try
            {
                var deleted = await _service.DeletePantryItemAsync(id, ct);
                if (deleted)
                {
                    _logger.LogInformation("{Method}: Pantry item with ID {Id} deleted.", method, id);
                    _logger.LogInformation("{Method}: Exiting successfully.", method);
                    return NoContent();
                }

                _logger.LogWarning("{Method}: Pantry item with ID {Id} not found for deletion.", method, id);
                _logger.LogInformation("{Method}: Exiting with NotFound. id={Id}", method, id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Deletes multiple pantry items by their IDs.
        /// </summary>
        /// <param name="request">A collection of pantry item IDs to delete.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns Ok with the ids of deleted items, otherwise 404 if none are found.</remarks>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(DeleteRequest), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeleteRequest>> DeleteItemsAsync([FromBody, BindRequired] DeleteRequest request, CancellationToken ct)
        {
            const string method = nameof(DeleteItemsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. request={Request}", method, nameof(PantryItemController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Delete request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("Delete request object is required.");
            }

            try
            {
                var deleted = await _service.DeletePantryItemsAsync(request.Ids, ct);
                if (deleted.Ids.Any())
                {
                    _logger.LogInformation("{Method}: Deleted {Count} pantry items.", method, deleted.Ids.Count());
                    _logger.LogInformation("{Method}: Exiting successfully.", method);
                    return StatusCode(204, deleted);
                }

                _logger.LogWarning("{Method}: No pantry items deleted for IDs: {Ids}", method, request.Ids);
                _logger.LogInformation("{Method}: Exiting with NotFound. ids={Ids}", method, request.Ids);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Searches the current user's pantry items for the given name, with pagination.
        /// </summary>
        /// <param name="query">The search term to query on.</param>
        /// <param name="take">The number of responses to return for pagination.</param>
        /// <param name="skip">The number of responses to skip for pagination.</param>
        /// <param name="ct">Cancellation token, unseen by user.</param>
        /// <remarks>Returns the pantry items found, along with the total number of responses.</remarks>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> SearchAsync([FromQuery, BindRequired] string query, int? take = null, int? skip = null, CancellationToken ct = default)
        {
            const string method = nameof(SearchAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. query={Query}, take={Take}, skip={Skip}", method, nameof(PantryItemController), query, take, skip);
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("{Method}: A search term is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. query=null or empty", method);
                return BadRequest("A search term is required.");
            }

            var userId = GetUserId();
            try
            {
                var results = await _service.Search(query, userId, take, skip, ct);
                if (results == null)
                {
                    _logger.LogWarning("{Method}: Service returned null search results.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null search results.");
                }

                _logger.LogInformation("{Method}: Search completed successfully. TotalCount={Count}", method, results.TotalCount);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
        public async Task<ActionResult<PantryItemDto>> UpdateAsync(string id, [FromBody, BindRequired] CreateUpdatePantryItemRequestDto pantryItem, CancellationToken ct)
        {
            const string method = nameof(UpdateAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}, pantryItem={PantryItem}", method, nameof(PantryItemController), id, pantryItem);
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("{Method}: id is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id=null or empty", method);
                return BadRequest("id is required.");
            }

            if (pantryItem == null)
            {
                _logger.LogWarning("{Method}: PantryItemDto pantryItem is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. pantryItem=null", method);
                return BadRequest("PantryItemDto pantryItem is required.");
            }

            var userId = GetUserId();
            try
            {
                var updated = await _service.UpdatePantryItemAsync(pantryItem, userId, ct);
                if (updated == null)
                {
                    _logger.LogWarning("{Method}: Service returned null updated pantry item.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null updated pantry item.");
                }

                _logger.LogInformation("{Method}: Pantry item updated successfully. id={Id}", method, updated.Id);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Searches the current user's pantry items for the given name.
        /// </summary>
        /// <param name="id">The id of the pantryItem to update</param>
        /// <param name="pantryItem">The pantry item to update</param>
        /// <returns>The pantry items found, or 400 if an error occurs.</returns>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PantryItemDto>> Update(string id, [FromBody] CreatePantryItemDto pantryItem)
        {
            if (string.IsNullOrEmpty(id))
            {
                _logger.LogWarning("id is required.");
                return BadRequest("id is required.");
            }

            if (pantryItem == null)
            {
                _logger.LogWarning("PantryItemDto pantryItem is required");
                return BadRequest("PantryItemDto pantryItem is required.");
            }

            var userId = GetUserId();
            return Ok(await _service.UpdatePantryItemAsync(pantryItem, userId));
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!, CultureInfo.InvariantCulture);
        }
    }
}