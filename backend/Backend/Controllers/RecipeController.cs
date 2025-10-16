using Backend.DTOs;
using Backend.Helpers;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller to handle Recipe CRUD as well as search and extraction based on a URL.
    /// </summary>
    /// <param name="recipeService">The underlying recipe service to handle business logic.</param>
    /// <param name="logger">A logger instance.</param>
    /// <param name="extractor">The recipe extractor to handle extracting recipes based on a URL.</param>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RecipeController(IRecipeService recipeService, ILogger<RecipeController> logger, IRecipeExtractor extractor) : PlannerControllerBase(logger)
    {
        private readonly IRecipeService _recipeService = recipeService;
        private readonly ILogger<RecipeController> _logger = logger;
        private readonly IRecipeExtractor _extractor = extractor;

        /// <summary>
        /// Creates a new recipe on the server, given a CreateRecipeDto object.
        /// </summary>
        /// <param name="request">The requested recipe to be created.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns 201 on creation</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> CreateAsync([FromBody, BindRequired] CreateUpdateRecipeDtoRequest request, CancellationToken ct)
        {
            const string method = nameof(CreateAsync);
            _logger.LogInformation("{Method}: Entering {Controller}", method, nameof(RecipeController));
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
            if (SanitizeAndCheckIngredients(method, request.Ingredients) is { } check2) return check2;

            request.Source = SanitizeInput(request.Source);
            request.Title = SanitizeInput(request.Title);

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var created = await _recipeService.CreateAsync(request, userId, ct);
                if (ResultNullCheck(method, created) is { } check3) return check3;

                _logger.LogInformation("{Method}: Recipe created with ID {Id}", method, created.Id);
                return Created("", created);
            });
        }

        /// <summary>
        /// Returns a recipe based on a given id.
        /// </summary>
        /// <param name="id">The id to return the recipe of.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns the found recipe. Ok on success, 404 on failure.</remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> GetByIdAsync(int id, CancellationToken ct)
        {
            const string method = nameof(GetByIdAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(RecipeController), id);

#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var r = await _recipeService.GetByIdAsync(id, userId, ct);
                if (ResultNullCheck(method, r, ret: NotFound) is { } check2) return check2;

                _logger.LogInformation("{Method}: Recipe retrieved. id={Id}", method, r!.Id);
                return Ok(r);
            });
        }

        /// <summary>
        /// Returns a list of recipes based on a list of ids.
        /// </summary>
        /// <param name="request">The list of ids to return.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns a list of full recipe objects. Ok on success.</remarks>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetRecipesResult>> GetByIdsAsync([FromBody, BindRequired] GetRecipesRequest request, CancellationToken ct)
        {
            const string method = nameof(GetByIdsAsync);
            _logger.LogInformation("{Method}: Entering {Controller}", method, nameof(RecipeController));
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
            if (CheckForNullOrEmpty(method, request.Ids, nameof(request.Ids)) is { } check2) return check2;

            foreach (var id in request.Ids)
                if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check3) return check3;

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var r = await _recipeService.GetByIdsAsync(request.Ids, userId, ct);
                if (ResultNullCheck(method, r) is { } check4) return check4;

                _logger.LogInformation("{Method}: Retrieved {Count} recipes.", method, r.TotalCount);
                return Ok(r);
            });
        }

        /// <summary>
        /// Searches recipes based on title and/or ingredients. Can be paged.
        /// </summary>
        /// <param name="title">The string to search in the title property for.</param>
        /// <param name="ingredient">The string to search in the ingredients for.</param>
        /// <param name="skip">The number of responses to skip for paging.</param>
        /// <param name="take">the number of responses to take for paging.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns a list of full recipe objects. Ok on success.</remarks>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetRecipesResult>> SearchAsync([FromQuery] string? title = null, [FromQuery] string? ingredient = null, [FromQuery] int? skip = null, [FromQuery] int? take = null, CancellationToken ct = default)
        {
            title = SanitizeInput(title);
            ingredient = SanitizeInput(ingredient);
            const string method = nameof(SearchAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. title={Title}, ingredient={Ingredient}, skip={Skip}, take={Take}", method, nameof(RecipeController), title, ingredient, skip, take);

            if (title == null && ingredient == null)
            {
                _logger.LogWarning("{Method}: At least one of title or ingredient must be provided for search.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. title=null, ingredient=null", method);
                return BadRequest("At least one of title or ingredient must be provided for search.");
            }

            if (CheckForLessThan0(method, skip, nameof(skip)) is { } check) return check;
#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, take, nameof(take)) is { } check2) return check2;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var r = await _recipeService.SearchAsync(userId, title, ingredient, skip, take, ct);
                if (ResultNullCheck(method, r) is { } check3) return check3;

                _logger.LogInformation("{Method}: Search returned {Count} recipes.", method, r.TotalCount);
                return Ok(r);
            });
        }

        /// <summary>
        /// Updates a recipe with the given id and recipe object.
        /// </summary>
        /// <param name="id">The id of the recipe to update.</param>
        /// <param name="request">The recipe object to update with.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns the updated recipe object. Ok on success, 404 if the recipe cannot be found.</remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> UpdateAsync(int id, [FromBody, BindRequired] CreateUpdateRecipeDtoRequest request, CancellationToken ct)
        {
            const string method = nameof(UpdateAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(RecipeController), id);
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;

            if (SanitizeAndCheckIngredients(method, request.Ingredients) is { } check2) return check2;
            request.Source = SanitizeInput(request.Source);
            request.Title = SanitizeInput(request.Title);

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var updated = await _recipeService.UpdateAsync(id, request, userId, ct);
                if (ResultNullCheck(method, updated, ret: NotFound) is { } check3) return check3;

                _logger.LogInformation("{Method}: Recipe updated. id={Id}", method, updated.Id);
                return Ok(updated);
            });
        }

        /// <summary>
        /// Deletes a recipe with the given id
        /// </summary>
        /// <param name="id">The id of the recipe to be deleted.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>Returns 201 if deleted, 404 if not found.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> DeleteAsync(int id, CancellationToken ct)
        {
            const string method = nameof(DeleteAsync);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(RecipeController), id);

#pragma warning disable IDE0046 // Convert to conditional expression
            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;
#pragma warning restore IDE0046 // Convert to conditional expression

            return await TryCallToServiceAsync(method, async () =>
            {
                var userId = GetUserId();
                var ok = await _recipeService.DeleteAsync(id, userId, ct);
                if (ResultNullCheck(method, ok ? "" : null, ret: NotFound) is { } check2) return check2;

                _logger.LogInformation("{Method}: Recipe with ID {Id} deleted.", method, id);
                return NoContent();
            });
        }

        /// <summary>
        /// Attempts to extract a recipe from a URL. The returned recipe is a draft that the user can then modify and save.
        /// Recipes is pulled from the URL using OpenGraph, schema.org, and microdata standards. If no recipe can be found,
        /// OK is still returned and the user can still save the URL if they wish. If the URL is invalid or there is an
        /// error during extraction, a 500 error is returned. The recipe is not saved to the database until the user
        /// verifies and saves it.
        /// </summary>
        /// <param name="request">The URL to source the recipe from.</param>
        /// <param name="ct">A token to cancel the operation.</param>
        /// <remarks>returns a recipe object for the user to verify. Does not insert recipe into the database. OK on success.</remarks>
        [HttpPost("extract")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExtractedRecipe>> ExtractRecipeAsync([FromBody, BindRequired] ExtractRequest request, CancellationToken ct)
        {
            const string method = nameof(ExtractRecipeAsync);
            _logger.LogInformation("{Method}: Entering {Controller}.", method, nameof(RecipeController));
            if (CheckForNull(method, request, nameof(request)) is { } check) return check;
            if (CheckForNull(method, request.Source, nameof(request.Source)) is { } check2) return check2;

            request.Source = SanitizeInput(request.Source);

            return await TryCallToServiceAsync(method, async () =>
            {
                _logger.LogInformation("{Method}: Extracting recipe from source URL: {Source}", method, request.Source);
                var draft = await _extractor.ExtractRecipeAsync(request.Source, ct);
                if (ResultNullCheck(method, draft) is { } check3) return check3;

                _logger.LogInformation("{Method}: Recipe extracted from source.", method);
                return Ok(draft);
            });
        }

        /// <summary>
        /// Returns all pantry items used while cooking this recipe. These can be deleted by the user if they wish.
        /// Quantity is not adjusted automatically, as the user may have multiple of the same item in their pantry.
        /// This tool is not sophisticated to determine quantity based on unit types, so the user must manually
        /// adjust quantities or delete items as they see fit. If the recipe has already been marked as cooked,
        /// this is a no-op and will simply return the pantry items again.
        /// </summary>
        /// <param name="id">The id of the recipe that is being cooked.</param>
        /// <remarks>returns a list of pantry items to possibly be deleted by the user now that the recipe has been made.</remarks>
        [HttpPut("{id}/cook")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<GetPantryItemsResult> CookRecipe(int id)
        {
            const string method = nameof(CookRecipe);
            _logger.LogInformation("{Method}: Entering {Controller}. id={Id}", method, nameof(RecipeController), id);

            if (CheckForLessThanOrEqualTo0(method, id, nameof(id)) is { } check) return check;

            var userId = GetUserId();
            return TryCallToService(method, () =>
            {
                var result = _recipeService.CookRecipe(id, userId);
                if (ResultNullCheck(method, result) is { } check2) return check2;

                _logger.LogInformation("{Method}: CookRecipe completed successfully. TotalCount={Count}", method, result.TotalCount);
                return Ok(result);
            });
        }

        private ActionResult? SanitizeAndCheckIngredients(string method, List<CreateUpdateRecipeIngredientDto> ingredients)
        {
            foreach (var ing in ingredients)
            {
                ing.Unit = SanitizeInput(ing.Unit);
                if (ing.Food is NewFoodReferenceDto newFood)
                {
                    if (CheckForNull(method, newFood.Name, nameof(newFood.Name)) is { } check2) return check2;
                    if (CheckForLessThanOrEqualTo0(method, newFood.CategoryId, nameof(newFood.CategoryId)) is { } check3) return check3;
                    newFood.Name = SanitizeInput(newFood.Name);
                }
                else if (ing.Food is ExistingFoodReferenceDto existingFood)
                    if (CheckForLessThanOrEqualTo0(method, existingFood.Id, nameof(existingFood.Id)) is { } check4) return check4;
            }

            return null;
        }
    }
}