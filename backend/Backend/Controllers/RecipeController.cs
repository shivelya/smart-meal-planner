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
        /// <param name="req">The requested recipe to be created.</param>
        /// <remarks>Returns 201 on creation</remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> Create([FromBody, BindRequired] CreateUpdateRecipeDtoRequest req)
        {
            try
            {
                _logger.LogInformation("Creating recipe for user {UserId}: {@Req}", GetUserId(), req);
                var created = await _recipeService.CreateAsync(req, GetUserId());
                _logger.LogInformation("Recipe created with ID {Id}", created.Id);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Create");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Returns a recipe based on a given id.
        /// </summary>
        /// <param name="id">The id to return the recipe of.</param>
        /// <remarks>Returns the found recipe. Ok on success, 404 on failure.</remarks>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> GetById(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving recipe with ID {Id} for user {UserId}", id, GetUserId());
                var r = await _recipeService.GetByIdAsync(id, GetUserId());
                if (r is null)
                {
                    _logger.LogWarning("Recipe with ID {Id} not found", id);
                    return NotFound();
                }
                _logger.LogInformation("Recipe retrieved: {@Recipe}", r);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetById");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Returns a list of recipes based on a list of ids.
        /// </summary>
        /// <param name="request">The list of ids to return.</param>
        /// <remarks>Returns a list of full recipe objects. Ok on success.</remarks>
        [HttpPost("bulk")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetRecipesResult>> GetByIds([FromBody, BindRequired] GetRecipesRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("Request object is required.");
                return BadRequest("request object is required.");
            }

            if (request.Ids == null)
            {
                _logger.LogWarning("Ids are required.");
                return BadRequest("List of ids is required.");
            }

            try
            {

                _logger.LogInformation("Retrieving recipes with IDs {@Ids} for user {UserId}", request.Ids, GetUserId());
                var r = await _recipeService.GetByIdsAsync(request.Ids, GetUserId());
                _logger.LogInformation("Retrieved {Count} recipes", r.TotalCount);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetByIds");
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
        /// <remarks>Returns a list of full recipe objects. Ok on success.</remarks>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetRecipesResult>> Search([FromQuery] string? title, [FromQuery] string? ingredient, [FromQuery] int? skip, [FromQuery] int? take)
        {
            try
            {
                _logger.LogInformation("Searching recipes for user {UserId}: title={Title}, ingredient={Ingredient}, skip={Skip}, take={Take}", GetUserId(), title, ingredient, skip, take);
                var r = await _recipeService.SearchAsync(new RecipeSearchOptions
                {
                    TitleContains = title,
                    IngredientContains = ingredient,
                    Skip = skip,
                    Take = take
                }, GetUserId());
                _logger.LogInformation("Search returned {Count} recipes", r.TotalCount);
                return Ok(r);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Search");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Updates a recipe with the given id and recipe object.
        /// </summary>
        /// <param name="id">The id of the recipe to update.</param>
        /// <param name="req">The recipe object to update with.</param>
        /// <remarks>Returns the updated recipe object. Ok on success, 404 if the recipe cannot be found.</remarks>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> Update(int id, [FromBody, BindRequired] CreateUpdateRecipeDtoRequest req)
        {
            try
            {
                _logger.LogInformation("Updating recipe with ID {Id} for user {UserId}: {@Req}", id, GetUserId(), req);
                var updated = await _recipeService.UpdateAsync(id, req, GetUserId());
                if (updated is null)
                {
                    _logger.LogWarning("Recipe with ID {Id} not found for update", id);
                    return NotFound();
                }
                _logger.LogInformation("Recipe updated: {@Recipe}", updated);
                return Ok(updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Update");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Deletes a recipe with the given id
        /// </summary>
        /// <param name="id">The id of the recipe to be deleted.</param>
        /// <remarks>Returns 201 if deleted, 404 if not found.</remarks>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                _logger.LogInformation("Deleting recipe with ID {Id} for user {UserId}", id, GetUserId());
                var ok = await _recipeService.DeleteAsync(id, GetUserId());
                if (ok)
                {
                    _logger.LogInformation("Recipe with ID {Id} deleted", id);
                    return NoContent();
                }
                _logger.LogWarning("Recipe with ID {Id} not found for deletion", id);
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Delete");
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Attempts to extract a recipe from a URL.
        /// </summary>
        /// <param name="request">The URL to source the recipe from.</param>
        /// <remarks>returns a recipe object for the user to verify. Does not insert recipe into the database. OK on success.</remarks>
        [HttpPost("extract")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExtractedRecipe>> ExtractRecipe([FromBody, BindRequired] ExtractRequest request)
        {
            if (request == null)
            {
                _logger.LogWarning("Request object is required.");
                return BadRequest("Request object is required.");
            }

            if (request.Source == null)
            {
                _logger.LogWarning("Source URL is required.");
                return BadRequest("Source URL is required.");
            }

            try
            {
                _logger.LogInformation("Extracting recipe from source URL: {Source}", request.Source);
                var draft = await _extractor.ExtractRecipeAsync(request.Source);
                _logger.LogInformation("Recipe extracted from source");
                return Ok(draft);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ExtractRecipe");
                return StatusCode(500, ex.Message);
            }
        }

        private int GetUserId()
        {
            return int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
        }
    }
}