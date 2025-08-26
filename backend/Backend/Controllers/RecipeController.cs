using System.Security.Claims;
using Backend.DTOs;
using Backend.Helpers;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
        /// <returns>201 on creation</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> Create([FromBody] CreateRecipeDto req)
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
        /// <returns>The found recipe. Ok on success, 404 on failure.</returns>
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
        /// <param name="ids">The list of ids to return.</param>
        /// <returns>A list of full recipe objects. Ok on success.</returns>
        [HttpPost("batch")] // body: { "ids": ["..."] }
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RecipeDto>>> GetByIds([FromBody] int[] ids)
        {
            try
            {
                _logger.LogInformation("Retrieving recipes with IDs {@Ids} for user {UserId}", ids, GetUserId());
                var r = await _recipeService.GetByIdsAsync(ids, GetUserId());
                _logger.LogInformation("Retrieved {Count} recipes", r.Count());
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
        /// <returns>A list of full recipe objects. Ok on success.</returns>
        [HttpGet("search")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyList<RecipeDto>>> Search([FromQuery] string? title, [FromQuery] string? ingredient, [FromQuery] int? skip, [FromQuery] int? take)
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
                _logger.LogInformation("Search returned {Count} recipes", r.Count());
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
        /// <returns>The updated recipe object. Ok on success, 404 if the recipe cannot be found.</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<RecipeDto>> Update(int id, [FromBody] UpdateRecipeDto req)
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
        /// <returns>201 if deleted, 404 if not found.</returns>
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
        /// <param name="source">The URL to source the recipe from.</param>
        /// <returns>A recipe object for the user to verify. Does not insert recipe into the database. OK on success.</returns>
        [HttpPost("extract")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExtractedRecipe>> ExtractRecipe([FromBody] string source)
        {
            try
            {
                _logger.LogInformation("Extracting recipe from source URL: {Source}", source);
                var draft = await _extractor.ExtractRecipeAsync(source);
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