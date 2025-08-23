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
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PantryItemController : ControllerBase
    {
        private readonly IPantryItemService _service;
        private readonly ILogger<PantryItemController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PantryItemController"/> class.
        /// </summary>
        /// <param name="service">The pantry item service.</param>
        /// <param name="logger">The logger instance.</param>
        public PantryItemController(IPantryItemService service, ILogger<PantryItemController> logger)
        {
            _service = service;
            _logger = logger;
        }

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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            _logger.LogInformation("Adding pantry item for user {UserId}: {@Dto}", userId, dto);
            try
            {
                var result = await _service.CreatePantryItemAsync(dto, userId);

                _logger.LogInformation("Pantry item added for user {UserId}: {@Result}", userId, result);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
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
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            _logger.LogInformation("Adding multiple pantry items for user {UserId}: {@Dtos}", userId, dtos);
            var result = await _service.CreatePantryItemsAsync(dtos, userId);
            _logger.LogInformation("Pantry items added for user {UserId}: {@Result}", userId, result);
            return Ok(result);
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
            var item = await _service.GetPantryItemByIdAsync(id);
            if (item is null)
            {
                _logger.LogWarning("Pantry item with ID {Id} not found", id);
                return NotFound();
            }
            _logger.LogInformation("Pantry item retrieved: {@Item}", item);
            return Ok(item);
        }

        /// <summary>
        /// Retrieves all pantry items for the authenticated user with pagination.
        /// </summary>
        /// <param name="pageNumber">The page number to retrieve.</param>
        /// <param name="pageSize">The number of items per page.</param>
        /// <returns>An object containing the total count and the items.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<GetItemsResult>> GetItems([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            _logger.LogInformation("Retrieving pantry items: page {PageNumber}, size {PageSize}", pageNumber, pageSize);
            var (items, totalCount) = await _service.GetAllPantryItemsAsync(pageNumber, pageSize);
            _logger.LogInformation("Retrieved {TotalCount} pantry items", totalCount);
            return Ok(new GetItemsResult() { TotalCount = totalCount,  Items = items });
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
            var deleted = await _service.DeletePantryItemAsync(id);
            if (deleted)
            {
                _logger.LogInformation("Pantry item with ID {Id} deleted", id);
                return NoContent();
            }
            _logger.LogWarning("Pantry item with ID {Id} not found for deletion", id);
            return NotFound();
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
            var deleted = await _service.DeletePantryItemsAsync(ids);
            if (deleted > 0)
            {
                _logger.LogInformation("Deleted {Count} pantry items", deleted);
                return Ok(deleted);
            }
            _logger.LogWarning("No pantry items deleted for IDs: {@Ids}", ids);
            return NotFound();
        }
    }

    public class GetItemsResult
    {
        public int TotalCount { get; set; }
        public required IEnumerable<PantryItemDto> Items { get; set; }
    }
}