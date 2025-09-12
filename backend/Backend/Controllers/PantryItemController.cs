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
        /// <remarks>Return a created pantry item DTO.</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> AddItemAsync(CreateUpdatePantryItemRequestDto dto)
        {
            if (dto == null)
            {
                _logger.LogWarning("PantryItemDto dto is required");
                return BadRequest("PantryItemDto dto is required.");
            }

            var userId = GetUserId();
            _logger.LogInformation("Adding pantry item for user {UserId}: {@Dto}", userId, dto);
            try
            {
                var result = await _service.CreatePantryItemAsync(dto, userId);

                _logger.LogInformation("Pantry item added for user {UserId}: {@Result}", userId, result);
                return CreatedAtAction(nameof(GetItemAsync), result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding pantry item.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Adds multiple pantry items for the authenticated user. Assumes a FoodId in the Food property for foods that already exist,
        /// and assumes a FoodName and CategoryId in the Food property for foods that need to be added.
        /// </summary>
        /// <param name="dtos">A collection of DTOs containing pantry item details.</param>
        /// <remarks>Return a collection of created pantry item DTOs.</remarks>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> AddItemsAsync(IEnumerable<CreateUpdatePantryItemRequestDto> dtos)
        {
            if (dtos == null || !dtos.Any())
            {
                _logger.LogWarning("A collection of PantryItemDto dtos is required.");
                return BadRequest("A collection of PantryItemDto dtos is required.");
            }

            var userId = GetUserId();
            _logger.LogInformation("Adding multiple pantry items for user {UserId}: {@Dtos}", userId, dtos);
            try
            {
                var result = await _service.CreatePantryItemsAsync(dtos, userId);
                _logger.LogInformation("Pantry items added for user {UserId}: {@Result}", userId, result);

                return CreatedAtAction(nameof(GetItemAsync), result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error adding pantry items");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Retrieves a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <remarks>Returns the pantry item DTO if found, otherwise 404 Not Found.</remarks>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> GetItemAsync(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("A valid pantry item ID is required.");
                return BadRequest("A valid pantry item ID is required.");
            } 

            _logger.LogInformation("Retrieving pantry item with ID {Id}", id);
            try
            {
                var item = await _service.GetPantryItemByIdAsync(id);
                if (item is null)
                {
                    _logger.LogWarning("Pantry item with ID {Id} not found", id);
                    return NotFound();
                }

                _logger.LogInformation("Pantry item retrieved: {@Item}", item);
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving pantry item.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Retrieves all pantry items for the authenticated user with pagination.
        /// </summary>
        /// <param name="skip">The page number to retrieve.</param>
        /// <param name="take">The number of items per page.</param>
        /// <remarks>Return a GetPantryItemsResult object containing the total count and fully constituted items.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> GetItemsAsync([FromQuery, BindRequired] int skip = 0, [FromQuery, BindRequired] int take = 10)
        {
            _logger.LogInformation("Retrieving pantry items: page {Size}, size {Take}", skip, take);
            try
            {
                var result = await _service.GetAllPantryItemsAsync(skip, take);
                _logger.LogInformation("Retrieved {TotalCount} pantry items", result.TotalCount);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error retrieving pantry items.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Deletes a pantry item by its unique ID.
        /// </summary>
        /// <param name="id">The pantry item's unique identifier.</param>
        /// <remarks> Returns No content if deleted, otherwise 404 Not Found.</remarks>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteItemAsync(int id)
        {
            _logger.LogInformation("Deleting pantry item with ID {Id}", id);
            try
            {
                var deleted = await _service.DeletePantryItemAsync(id);
                if (deleted)
                {
                    _logger.LogInformation("Pantry item with ID {Id} deleted", id);
                    return NoContent();
                }

                _logger.LogWarning("Pantry item with ID {Id} not found for deletion", id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while deleting pantry item.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Deletes multiple pantry items by their IDs.
        /// </summary>
        /// <param name="request">A collection of pantry item IDs to delete.</param>
        /// <remarks>Returns Ok with the ids of deleted items, otherwise 404 if none are found.</remarks>
        [HttpDelete("bulk")]
        [ProducesResponseType(typeof(DeleteRequest), StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DeleteRequest>> DeleteItemsAsync([FromBody, BindRequired] DeleteRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("Delete request object is required.");
                return BadRequest("Delete request object is required.");
            }

            _logger.LogInformation("Deleting multiple pantry items: {@Ids}", request.Ids);
            try
            {
                var deleted = await _service.DeletePantryItemsAsync(request.Ids);
                if (deleted.Ids.Any())
                {
                    _logger.LogInformation("Deleted {Count} pantry items", deleted.Ids.Count());
                    return StatusCode(204, deleted);
                }

                _logger.LogWarning("No pantry items deleted for IDs: {Ids}", request.Ids);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting pantry items.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Searches the current user's pantry items for the given name, with pagination.
        /// </summary>
        /// <param name="query">The search term to query on.</param>
        /// <param name="take">The number of responses to return for pagination.</param>
        /// <param name="skip">The number of responses to skip for pagination.</param>
        /// <remarks>Returns the pantry items found, along with the total number of responses.</remarks>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> SearchAsync([FromQuery, BindRequired] string query, int? take = null, int? skip = null)
        {
            if (string.IsNullOrEmpty(query))
            {
                _logger.LogWarning("A search term is required.");
                return BadRequest("A search term is required.");
            }

            var userId = GetUserId();
            try
            {
                var results = await _service.Search(query, userId, take, skip);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while search for pantry item.)");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Updates a given pantry item in the DB.
        /// </summary>
        /// <param name="id">The id of the pantryItem to update</param>
        /// <param name="pantryItem">The pantry item to update</param>
        /// <remarks>Returns the updated pantry item DTO, or 400 if an error occurs.</remarks>
        [HttpPut("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PantryItemDto>> UpdateAsync(string id, [FromBody, BindRequired] CreateUpdatePantryItemRequestDto pantryItem)
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
            try
            {
                return Ok(await _service.UpdatePantryItemAsync(pantryItem, userId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while updating pantry item.");
                return StatusCode(500, ex);
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}