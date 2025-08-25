using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// Adds a pantry item for the authenticated user. Assumes an IngredientId for ingredients that already exist,
        /// and assumes an Ingredientname and CategoryId for ingredients that need to be added.
        /// </summary>
        /// <param name="dto">A DTO containing pantry item details.</param>
        /// <returns>A created pantry item DTO.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<PantryItemDto>> AddItem(CreatePantryItemDto dto)
        {
            var userId = GetUserId();
            _logger.LogInformation("Adding pantry item for user {UserId}: {@Dto}", userId, dto);
            try
            {
                var result = await _service.CreatePantryItemAsync(dto, userId);

                _logger.LogInformation("Pantry item added for user {UserId}: {@Result}", userId, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding pantry item.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Adds multiple pantry items for the authenticated user. Assumes an IngredientId for ingredients which already exist,
        /// and assumes an IngredientName and a CategoryId for ingredients which need to be added.
        /// </summary>
        /// <param name="dtos">A collection of DTOs containing pantry item details.</param>
        /// <returns>A collection of created pantry item DTOs.</returns>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> AddItems(IEnumerable<CreatePantryItemDto> dtos)
        {
            var userId = GetUserId();
            _logger.LogInformation("Adding multiple pantry items for user {UserId}: {@Dtos}", userId, dtos);
            try
            {
                var result = await _service.CreatePantryItemsAsync(dtos, userId);
                _logger.LogInformation("Pantry items added for user {UserId}: {@Result}", userId, result);

                return Ok(result);
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
        /// <returns>The pantry item DTO if found, otherwise 404 Not Found.</returns>
        [HttpGet("{id:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PantryItemDto>> GetItem(int id)
        {
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
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An object containing the total count and the items.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<GetPantryItemsResult>> GetItems([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Retrieving pantry items: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
            try
            {
                var (items, totalCount) = await _service.GetAllPantryItemsAsync(pageNumber, pageSize);
                _logger.LogInformation("Retrieved {TotalCount} pantry items", totalCount);

                return Ok(new GetPantryItemsResult() { TotalCount = totalCount, Items = items });
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
        /// <returns>No content if deleted, otherwise 404 Not Found.</returns>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteItem(int id)
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
        /// <param name="ids">A collection of pantry item IDs to delete.</param>
        /// <returns>Ok with the number of deleted items, otherwise 404 Not Found.</returns>
        [HttpDelete("bulk")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteItems([FromBody] IEnumerable<int> ids)
        {
            _logger.LogInformation("Deleting multiple pantry items: {@Ids}", ids);
            try
            {
                var deleted = await _service.DeletePantryItemsAsync(ids);
                if (deleted > 0)
                {
                    _logger.LogInformation("Deleted {Count} pantry items", deleted);
                    return Ok(deleted);
                }

                _logger.LogWarning("No pantry items deleted for IDs: {@Ids}", ids);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error deleting pantry items.");
                return StatusCode(500, ex);
            }
        }

        /// <summary>
        /// Searches the current user's pantry items for the given name.
        /// </summary>
        /// <param name="search">The search term to query on.</param>
        /// <returns>The pantry items found, or 400 if an error occurs.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IEnumerable<PantryItemDto>>> Search([FromQuery] string search)
        {
            if (string.IsNullOrEmpty(search))
            {
                _logger.LogWarning("A search term is required.");
                return BadRequest("A search term is required.");
            }

            var userId = GetUserId();
            try
            {
                return Ok(await _service.Search(search, userId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error while search for pantry item.)");
                return StatusCode(500, ex);
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