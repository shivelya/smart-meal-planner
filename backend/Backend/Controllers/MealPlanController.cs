using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security;
using System.Security.Claims;
using Backend.DTOs;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MealPlanController(IMealPlanService service, ILogger<MealPlanController> logger, IConfiguration configuration) : ControllerBase
    {
        private readonly IMealPlanService _service = service;
        private readonly ILogger<MealPlanController> _logger = logger;
        private readonly int MAXDAYS = configuration.GetValue("MaxMealPlanGenerationDays", 14);

        /// <summary>
        /// Returns a list of all meal plans. Meals are included in the response but recipes are not and will needed loaeded separately.
        /// </summary>
        /// <param name="skip">The number of results to skip for pagination.</param>
        /// <param name="take">The number of results to take for pagination.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns a list of meal plans, as well as the total count.</remarks>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetMealPlansResult>> GetMealPlansAsync(int? skip = null, int? take = null, CancellationToken ct = default)
        {
            const string method = nameof(GetMealPlansAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. skip={Skip}, take={Take}", method, nameof(MealPlanController), skip, take);
            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.GetMealPlansAsync(userId, skip, take, ct);
            }, Ok, (plans) =>
                _logger.LogInformation("{Method}: GET for meal plans completed with {Count} results. PlanIds: {PlanIds}", method, plans.TotalCount, string.Join(",", plans.MealPlans.Select(p => p.Id)))
            );
        }

        /// <summary>
        /// Creates a new meal plan
        /// </summary>
        /// <param name="request">The meal plan to be created.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns the created meal plan object.</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MealPlanDto>> AddMealPlanAsync(CreateUpdateMealPlanRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(AddMealPlanAsync);
            _logger.LogInformation("{Method}: Entering", method);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request must be non-null.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("Request must be non-null.");
            }

            SanitizeMeals(request.Meals);

            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.AddMealPlanAsync(userId, request, ct);
            },
            (plan) => Created("", plan),
            (plan) =>
                _logger.LogInformation("{Method}: Created meal plan successfully. Id={Id}", method, plan.Id)
            );
        }

        /// <summary>
        /// Updates an existing meal plan
        /// </summary>
        /// <param name="id">The id of the meal plan to be updated.</param>
        /// <param name="request">The meal plan to be updated.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns the updated meal plan object.</remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MealPlanDto>> UpdateMealPlanAsync(int id, CreateUpdateMealPlanRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(UpdateMealPlanAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(MealPlanController), id);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request must be non-null.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id={Id}, request=null", method, id);
                return BadRequest("Request must be non-null.");
            }

            SanitizeMeals(request.Meals);

            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.UpdateMealPlanAsync(id, userId, request, ct);
            }, Ok, (plan) =>
                _logger.LogInformation("{Method}: Updated meal plan successfully. Id={Id}", method, plan.Id)
            );
        }

        /// <summary>
        /// Deletes an existing meal plan
        /// </summary>
        /// <param name="id">The id of the meal plan to be deleted.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns 204 on successful deletion, or 404 if not found.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteMealPlanAsync(int id, CancellationToken ct = default)
        {
            const string method = nameof(DeleteMealPlanAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(MealPlanController), id);

            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.DeleteMealPlanAsync(id, userId, ct);
            },
            (plan) => NoContent(),
            (plan) =>
                 _logger.LogInformation("{Method}: Deleted meal plan successfully. id={Id}", method, id)
            );
        }

        /// <summary>
        /// Generates a meal plan based on the user's pantry items. Is not inserted into the DB until the user verifies the list.
        /// Recipes will be chosen based on the user's pantry items, but if there are not enough recipes to fill the meal plan,
        /// recipes will be pulled from external sources. Recipes will not be duplicated within the generated meal plan.
        /// If the user has no recipes or pantry items, all recipes will be pulled from external sources. If the user chooses to
        /// use only external sources, no recipes from the user's account will be used. The user can then modify the plan
        /// as they see fit and save it. The generated meal plan will have no name or description, and the start date
        /// will be as specified in the request. If the user wants to change these, they can do so when saving the meal plan.
        /// The maximum number of days that can be generated is set in configuration, and defaults to 14. If the user
        /// requests more than this, a 400 error will be returned. Recipes are not eagerly loaded in the response
        /// to avoid returning too much data, so the user will need to load recipes separately if they want to see them.
        /// </summary>
        /// <param name="request">An object describing how to generate the meal plan. Includes a number of days to generate for,
        /// the start date for the meal plan, and whether the recipes should come only from external sources.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns a MealPlanDto object with the correct number of recipes. It will not be be inserted into the DB until
        /// the user verifies the list.</remarks>
        [HttpPost("generate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CreateUpdateMealPlanRequestDto>> GenerateMealPlanAsync(GenerateMealPlanRequestDto request, CancellationToken ct = default)
        {
            const string method = nameof(GenerateMealPlanAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. request={@Request}", method, nameof(MealPlanController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request must be non-null.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest.", method);
                return BadRequest("Request must be non-null.");
            }

            if (request.Days <= 0)
            {
                _logger.LogWarning("{Method}: Cannot create meal plan for less than 1 day.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request={@Request}", method, request);
                return BadRequest("Cannot create meal plan for less than 1 day.");
            }

            if (request.Days > MAXDAYS)
            {
                _logger.LogWarning("{Method}: User tried to create a meal plan for more than {Max} days.", method, MAXDAYS);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request={@Request}", method, request);
                return BadRequest($"Cannot create meal plan for more than {MAXDAYS} days,");
            }

            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.GenerateMealPlanAsync(request, userId, ct);
            }, Ok, (plan) =>
                 _logger.LogInformation("{Method}: Meal plan generated successfully. Days={Days}", method, request.Days)
            );
        }

        /// <summary>
        /// Returns all pantry items used while cooking this meal. These can be deleted by the user if they wish.
        /// Quantity is not adjusted automatically, as the user may have multiple of the same item in their pantry.
        /// This tool is not sophisticated to determine quantity based on unit types, so the user must manually
        /// adjust quantities or delete items as they see fit. If the meal has already been marked as cooked,
        /// this is a no-op and will simply return the pantry items again.
        /// </summary>
        /// <param name="id">The id of the meal plan the meal was taken from.</param>
        /// <param name="entryId">The id of the meal within the meal plan being cooked.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <remarks>Returns a list of pantry items to possibly be deleted by the user now that the meal has been made.</remarks>
        [HttpGet("{id}/cook/{entryId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPantryItemsResult>> CookMealAsync(int id, int entryId, CancellationToken ct = default)
        {
            const string method = nameof(CookMealAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}, mealEntryId={MealEntryId}", method, nameof(MealPlanController), id, entryId);
            if (id <= 0)
            {
                _logger.LogWarning("{Method}: Id must be positive.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id={Id}", method, id);
                return BadRequest("Id must be positive.");
            }

            if (entryId <= 0)
            {
                _logger.LogWarning("{Method}: mealEntryId must be positive.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. mealEntryId={MealEntryId}", method, entryId);
                return BadRequest("mealEntryId must be positive.");
            }

            return await CallToService(method, async () =>
            {
                var userId = GetUserId();
                return await _service.CookMealAsync(id, entryId, userId, ct);
            }, Ok, (result) =>
                 _logger.LogInformation("{Method}: CookMealAsync completed successfully. ItemsCount={Count}", method, result.TotalCount)
            );
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!, CultureInfo.InvariantCulture);
        }

        private async Task<ActionResult> CallToService<T>(string method, Func<Task<T>> doWork, Func<T, ActionResult> successReturn,
            Action<T> successLogs)
        {
            try
            {
                var result = await doWork();
                if (result == null)
                {
                    _logger.LogWarning("{Method}: Service returned null.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500);
                }

                successLogs(result);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return successReturn(result);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "{Method}: User does not have permission.", method);
                return Unauthorized();
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex, "{Method}: Non-existent ID for meal plan.", method);
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "{Method}: Could not update meal plan", method);
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500);
            }
        }

        private static void SanitizeMeals(IEnumerable<CreateUpdateMealPlanEntryRequestDto> meals)
        {
            foreach (var meal in meals)
                meal.Notes = SanitizeInput(meal.Notes);
        }

        private static string? SanitizeInput(string? input)
        {
            return input?.Replace(Environment.NewLine, "").Trim()!;
        }
    }
}