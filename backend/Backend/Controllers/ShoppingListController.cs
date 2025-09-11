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
        /// <remarks>Returns the shopping list.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<GetShoppingListResult> GetShoppingList()
        {
            try
            {
                var userId = GetUserId();
                var result = _service.GetShoppingList(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Generates a shopping list based on a given meal plan
        /// </summary>
        /// <param name="request">Includes a meal plan id and whether or not to restart the list.</param>
        /// <remarks>Returns a list of foods to shop for.</remarks>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetShoppingListResult>> GenerateAsync(GenerateShoppingListRequestDto request)
        {
            if (request == null)
            {
                _logger.LogWarning("request object is requried.");
                return BadRequest("request object is required.");
            }

            if (request.MealPlanId <= 0)
            {
                _logger.LogWarning("Valid meal plan id is required.");
                return BadRequest("Valid meal plan id is required.");
            }

            try
            {
                var userId = GetUserId();
                await _service.GenerateAsync(request, userId);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                return StatusCode(500, ex.Message);
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}