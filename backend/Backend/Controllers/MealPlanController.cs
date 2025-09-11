using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/meal-plan")]
    [Authorize]
    public class MealPlanController(IMealPlanService service, ILogger<MealPlanController> logger, IConfiguration configuration) : ControllerBase
    {
        private readonly IMealPlanService _service = service;
        private readonly ILogger<MealPlanController> _logger = logger;
        private readonly int MAXDAYS = configuration.GetValue("MaxMealPlanGenerationDays", 14);

        /// <summary>
        /// Returns a list of all meal plans
        /// </summary>
        /// <param name="skip">The number of results to skip for pagination.</param>
        /// <param name="take">The number of results to take for pagination.</param>
        /// <remarks>Returns a list of meal plans, as well as the total count.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetMealPlansResult>> GetMealPlansAsync(int? skip = null, int? take = null)
        {
            try
            {
                var plans = await _service.GetMealPlansAsync(skip, take);

                _logger.LogInformation("GET for meal plans completed with {count} results", plans.TotalCount);

                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not retrieve meal plans: {ex}", ex.Message);
                return StatusCode(500, "Could not retrieve meal plans: " + ex.Message);
            }
        }

        /// <summary>
        /// Creates a new meal plan
        /// </summary>
        /// <param name="request">The meal plan to be created.</param>
        /// <remarks>Returns the created meal plan object.</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MealPlanDto>> AddMealPlanAsync(CreateUpdateMealPlanRequestDto request)
        {
            if (request == null)
            {
                _logger.LogWarning("Request must be non-null");
                return BadRequest("Request must be non-null.");
            }

            try
            {
                var userId = GetUserId();
                var plan = await _service.AddMealPlanAsync(userId, request);

                _logger.LogInformation("Created meal plan successfully.");
                return CreatedAtAction(nameof(GetMealPlansAsync), plan);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not create meal plan: {ex}", ex.Message);
                return StatusCode(500, "Could not create meal plan: " + ex.Message);
            }
        }

        /// <summary>
        /// Updates an existing meal plan
        /// </summary>
        /// <param name="id">The id of the meal plan to be updated.</param>
        /// <param name="request">The meal plan to be updated.</param>
        /// <remarks>Returns the updated meal plan object.</remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MealPlanDto>> UpdateMealPlanAsync(int id, CreateUpdateMealPlanRequestDto request)
        {
            if (request == null)
            {
                _logger.LogWarning("Request must be non-null");
                return BadRequest("Request must be non-null.");
            }

            try
            {
                var userId = GetUserId();
                var plan = await _service.UpdateMealPlanAsync(id, userId, request);

                _logger.LogInformation("Updated meal plan successfully.");
                return Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not update meal plan: {ex}", ex.Message);
                return StatusCode(500, "Could not update meal plan: " + ex.Message);
            }
        }

        /// <summary>
        /// Deletes an existing meal plan
        /// </summary>
        /// <param name="id">The id of the meal plan to be deleted.</param>
        /// <remarks>Returns 204 on successful deletion, or 404 if not found.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteMealPlanAsync(int id)
        {
            try
            {
                var userId = GetUserId();
                if (await _service.DeleteMealPlanAsync(id, userId))
                {
                    _logger.LogInformation("Deleted meal plan successfully.");
                    return NoContent();
                }

                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not update meal plan: {ex}", ex.Message);
                return StatusCode(500, "Could not update meal plan: " + ex.Message);
            }
        }

        public class GenerateMealPlanRequestDto
        {
            public int Days { get; set; }
            public DateTime StartDate { get; set; }
            public bool UseExternal { get; set; }
        }

        /// <summary>
        /// Generates a meal plan based on the user's pantry items
        /// </summary>
        /// <param name="request">An object describing how to generate the meal plan. Includes a number of days to generate for,
        /// the start date for the meal plan, and whether the recipes should come only from external sources.</param>
        /// <remarks>Returns a MealPlanDto object with the correct number of recipes. It will not be be inserted into the DB until
        /// the user verifies the list.</remarks>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateUpdateMealPlanRequestDto>> GenerateMealPlanAsync(GenerateMealPlanRequestDto request)
        {
            if (request.Days <= 0)
            {
                _logger.LogWarning("Cannot create meal plan for less than 1 day.");
                return BadRequest("Cannot create meal plan for less than 1 day.");
            }

            if (request.Days > MAXDAYS)
                {
                    _logger.LogWarning("User tried to create a meal plan for more than {max} days.", MAXDAYS);
                    return BadRequest($"Cannot create meal plan for more than {MAXDAYS} days,");
                }

            try
            {
                var userId = GetUserId();
                var plan = await _service.GenerateMealPlanAsync(request.Days, userId, request.StartDate, request.UseExternal);

                _logger.LogInformation("Meal plan generated successfully.");
                return Ok(plan);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not generate meal plan: {ex}", ex.Message);
                return StatusCode(500, "Could not generate meal plan: " + ex.Message);
            }
        }

        /// <summary>
        /// Returns all pantry items used while cooking this meal.
        /// </summary>
        /// <param name="id">The id of the meal plan the meal was taken from.</param>
        /// <param name="mealEntryId">The id of the meal within the meal plan being cooked.</param>
        /// <remarks>Returns a list of pantry items to possibly be deleted by the user now that the meal has been made.</remarks>
        [HttpPut("cook/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> CookMeal(int id, [FromBody] int mealEntryId)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Id must be positive.");
                return BadRequest("Id must be positive.");
            }

            if (mealEntryId <= 0)
            {
                _logger.LogWarning("mealEntryId must be positive.");
                return BadRequest("mealEntryId must be positive.");
            }

            var userId = GetUserId();
            try
            {
                return Ok(await _service.CookMeal(id, mealEntryId, userId));
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