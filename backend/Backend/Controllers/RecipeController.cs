using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Security.Claims;
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
    public class RecipeController(IRecipeService recipeService, ILogger<RecipeController> logger, IRecipeExtractor extractor) : ControllerBase
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
            _logger.LogInformation("{Method}: Entering {Controller}. request={@Request}", method, nameof(RecipeController), request);

            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("request object is required.");
            }

            request.Source = SanitizeInput(request.Source);
            request.Title = SanitizeInput(request.Title);
            SanitizeIngredients(request.Ingredients);

            try
            {
                var userId = GetUserId();
                var created = await _recipeService.CreateAsync(request, userId, ct);
                if (created == null)
                {
                    _logger.LogWarning("{Method}: Service returned null created recipe.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null created recipe.");
                }
                _logger.LogInformation("{Method}: Recipe created with ID {Id}", method, created.Id);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Created("", created);
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "{Method}: Could not create recipe.", method);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, "Could not create recipe.");
            }
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
            try
            {
                var userId = GetUserId();
                var r = await _recipeService.GetByIdAsync(id, userId, ct);
                if (r is null)
                {
                    _logger.LogWarning("{Method}: Recipe with ID {Id} not found.", method, id);
                    _logger.LogInformation("{Method}: Exiting with NotFound. id={Id}", method, id);
                    return NotFound();
                }
                _logger.LogInformation("{Method}: Recipe retrieved. id={Id}", method, r.Id);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
            _logger.LogInformation("{Method}: Entering {Controller}. request={@Request}", method, nameof(RecipeController), request);
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("request object is required.");
            }

            if (request.Ids == null)
            {
                _logger.LogWarning("{Method}: Ids are required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. ids=null", method);
                return BadRequest("List of ids is required.");
            }

            try
            {
                var userId = GetUserId();
                var r = await _recipeService.GetByIdsAsync(request.Ids, userId, ct);
                if (r == null)
                {
                    _logger.LogWarning("{Method}: Service returned null recipes.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null recipes.");
                }
                _logger.LogInformation("{Method}: Retrieved {Count} recipes.", method, r.TotalCount);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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

            try
            {
                var userId = GetUserId();
                var r = await _recipeService.SearchAsync(userId, title, ingredient, skip, take, ct);
                if (r == null)
                {
                    _logger.LogWarning("{Method}: Service returned null search results.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null search results.");
                }
                _logger.LogInformation("{Method}: Search returned {Count} recipes.", method, r.TotalCount);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("Request object is required.");
            }

            request.Source = SanitizeInput(request.Source);
            request.Title = SanitizeInput(request.Title);
            SanitizeIngredients(request.Ingredients);

            try
            {
                var userId = GetUserId();
                var updated = await _recipeService.UpdateAsync(id, request, userId, ct);
                if (updated is null)
                {
                    _logger.LogWarning("{Method}: Recipe with ID {Id} not found for update.", method, id);
                    _logger.LogInformation("{Method}: Exiting with NotFound. id={Id}", method, id);
                    return NotFound();
                }
                _logger.LogInformation("{Method}: Recipe updated. id={Id}", method, updated.Id);
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
            try
            {
                var userId = GetUserId();
                var ok = await _recipeService.DeleteAsync(id, userId, ct);
                if (ok)
                {
                    _logger.LogInformation("{Method}: Recipe with ID {Id} deleted.", method, id);
                    _logger.LogInformation("{Method}: Exiting successfully.", method);
                    return NoContent();
                }
                _logger.LogWarning("{Method}: Recipe with ID {Id} not found for deletion.", method, id);
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
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. request=null", method);
                return BadRequest("Request object is required.");
            }

            request.Source = SanitizeInput(request.Source);

            if (request.Source == null)
            {
                _logger.LogWarning("{Method}: Source URL is required.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. source=null", method);
                return BadRequest("Source URL is required.");
            }

            try
            {
                _logger.LogInformation("{Method}: Extracting recipe from source URL: {Source}", method, request.Source);
                var draft = await _extractor.ExtractRecipeAsync(request.Source, ct);
                if (draft == null)
                {
                    _logger.LogWarning("{Method}: No recipe could be extracted from the provided URL: {Source}", method, request.Source);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                }
                _logger.LogInformation("{Method}: Recipe extracted from source.", method);
                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return Ok(draft);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Method}: Exception occurred. Message: {Message}, StackTrace: {StackTrace}", method, ex.Message, ex.StackTrace);
                _logger.LogInformation("{Method}: Exiting with error.", method);
                return StatusCode(500, ex.Message);
            }
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
            if (id <= 0)
            {
                _logger.LogWarning("{Method}: Id must be positive.", method);
                _logger.LogInformation("{Method}: Exiting with BadRequest. id={Id}", method, id);
                return BadRequest("Id must be positive.");
            }

            var userId = GetUserId();
            try
            {
                var result = _recipeService.CookRecipe(id, userId);
                if (result == null)
                {
                    _logger.LogWarning("{Method}: Service returned null pantry items.", method);
                    _logger.LogInformation("{Method}: Exiting with null result.", method);
                    return StatusCode(500, "Service returned null pantry items.");
                }
                _logger.LogInformation("{Method}: CookRecipe completed successfully. TotalCount={Count}", method, result.TotalCount);
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

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!, CultureInfo.InvariantCulture);
        }

        private static string SanitizeInput(string? input)
        {
            return input?.Replace(Environment.NewLine, "").Trim()!;
        }

        private static void SanitizeIngredients(List<CreateUpdateRecipeIngredientDto> ingredients)
        {
            foreach (var ing in ingredients)
            {
                ing.Unit = SanitizeInput(ing.Unit);
                if (ing.Food.GetType() == typeof(NewFoodReferenceDto))
                {
                    ((NewFoodReferenceDto)ing.Food).Name = SanitizeInput(((NewFoodReferenceDto)ing.Food).Name);
                }
            }
        }
    }
}