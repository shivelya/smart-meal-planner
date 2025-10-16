using System.ComponentModel.DataAnnotations;
using System.Security;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    public abstract class PlannerControllerBase(ILogger<PlannerControllerBase> logger) : ControllerBase
    {
        private readonly ILogger<PlannerControllerBase> _logger = logger;

        protected ActionResult? ResultNullCheck<T>(string method, T result, string? token = null, Func<ActionResult>? ret = null)
        {
            token ??= GetUserId().ToString();
            if (result == null)
            {
                _logger.LogError("{Method} failed: Server returned null.", method);
                _logger.LogInformation("Exiting {Method}: Identifying token={Token}", method, token);
                return ret != null ? ret() : StatusCode(StatusCodes.Status500InternalServerError);
            }

            return null;
        }

        /// <summary>
        /// A helper method to wrap calls to service methods, handling common exceptions and logging. Maps the following exceptions to HTTP responses:
        /// <list type="bullet">
        /// <item><description><see cref="ValidationException"/> -> 401 Unauthorized</description></item>
        /// <item><description><see cref="ArgumentException"/> -> 400 Bad Request</description></item>
        /// <item><description><see cref="SecurityException"/> -> 404 Not Found</description></item>
        /// <item><description><see cref="HttpRequestException"/> -> 503 Service Unavailable</description></item>
        /// <item><description>Other exceptions -> 500 Internal Server Error</description></item>
        /// </list>
        /// </summary>
        /// <param name="method">The name of the calling method, for logging purposes.</param>
        /// <param name="doWork">The actual work the calling method is trying to complete, usually a call to its service.</param>
        /// <param name="token">An optional identifying token. Uses the userId by default.</param>
        /// <returns>The result of <paramref name="doWork"/> on success. If an exception is thrown, the appropriate HTTP response code is returned.</returns>
        protected async Task<ActionResult> TryCallToServiceAsync(string method, Func<Task<ActionResult>> doWork, string? token = null)
        {
            token ??= GetUserId().ToString();
            try
            {
                var result = await doWork();

                _logger.LogInformation("{Method}: Exiting successfully.", method);
                return result;
            }
            catch (ValidationException ex)
            {
                _logger.LogWarning(ex, "{Method}: User does not have permission. Identifying token={Token}", method, token);
                return Unauthorized();
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "{Method}: Bad parameter. Identifying token={Token}", method, token);
                return BadRequest();
            }
            catch (SecurityException ex)
            {
                _logger.LogWarning(ex, "{Method}: NotFound. Identifying token={Token}", method, token);
                return NotFound();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "{Method}: External service threw an exception.", method);
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Method}", method);
                _logger.LogInformation("Exiting {Method}: Identifying Token={Token}", method, token);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Checks if the given request object is null and logs appropriately. Returns a BadRequest or the provided ActionResult if null.
        /// </summary>
        /// <typeparam name="T">The type of object being checked for null.</typeparam>
        /// <param name="method">The name of the method calling this function, for logging purposes.</param>
        /// <param name="toCheck">The object being checked for null.</param>
        /// <param name="valueName">the name of the object being checked, for logging and error handling purposes.</param>
        /// <param name="token">An optional identifying token for logging. Uses the userId by default.</param>
        /// <param name="ret">An optional return function. Uses BadReqeust by default.</param>
        /// <returns>Null if <paramref name="toCheck"/> is not null, or BadRequest or the given <paramref name="ret"/> if null.</returns>
        protected ActionResult? CheckForNull<T>(string method, T toCheck, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            token ??= GetUserId().ToString();
            if (toCheck == null)
            {
                _logger.LogWarning("{Method}: {ValueName} is null.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} is required.");
            }

            return null;
        }

        /// <summary>
        /// Checks if the given request object is null and logs appropriately. Returns a BadRequest or the provided ActionResult if null.
        /// </summary>
        /// <typeparam name="T">The type of object being checked for null.</typeparam>
        /// <param name="method">The name of the method calling this function, for logging purposes.</param>
        /// <param name="toCheck">The IEnumerable object being checked for null.</param>
        /// <param name="valueName">The name of the object being checked, for logging and error handling purposes.</param>
        /// <param name="token">An optional identifying token for logging. Uses the userId by default.</param>
        /// <param name="ret">An optional return function. Uses BadReqeust by default.</param>
        /// <returns>Null if <paramref name="toCheck"/> is not null and not empty, or BadRequest or the given <paramref name="ret"/> otherwise.</returns>
        protected ActionResult? CheckForNullOrEmpty<T>(string method, IEnumerable<T> toCheck, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            token ??= GetUserId().ToString();
            if (toCheck == null || !toCheck.Any())
            {
                _logger.LogWarning("{Method}: {ValueName} object is null.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} is required.");
            }

            return null;
        }

        /// <summary>
        /// Checks if the given string is null or whitespace and logs appropriately. Returns a BadRequest or the provided ActionResult if so.
        /// </summary>
        /// <param name="method">The name of the method calling this function, for logging purposes.</param>
        /// <param name="toCheck">The string being checked for null or whitespace.</param>
        /// <param name="valueName">The name of the object being checked, for logging and error handling purposes.</param>
        /// <param name="token">An optional identifying token for logging. Uses the userId by default.</param>
        /// <param name="ret">An optional return function. Uses BadReqeust by default.</param>
        /// <returns>Null if <paramref name="toCheck"/> is not null and not whitespace, or BadRequest or the given <paramref name="ret"/> otherwise.</returns>
        protected ActionResult? CheckForNullOrWhitespace(string method, string? toCheck, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            if (string.IsNullOrWhiteSpace(toCheck))
            {
                _logger.LogWarning("{Method}: {ValueName} is null or empty.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. Identifying Token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} is required.");
            }

            return null;
        }

        /// <summary>
        /// Checks if the given integer is less than 0. Returns a BadRequest or the provided ActionResult if so.
        /// </summary>
        /// <param name="method">The name of the method calling this function, for logging purposes.</param>
        /// <param name="toCheck">The integer being checked for less than zero.</param>
        /// <param name="valueName">The name of the object being checked, for logging and error handling purposes.</param>
        /// <param name="token">An optional identifying token for logging. Uses the userId by default.</param>
        /// <param name="ret">An optional return function. Uses BadReqeust by default.</param>
        /// <returns>Null if <paramref name="toCheck"/> is not less than zero, or BadRequest or the given <paramref name="ret"/> otherwise.</returns>
        protected ActionResult? CheckForLessThan0(string method, decimal? toCheck, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            if (toCheck < 0)
            {
                _logger.LogWarning("{Method}: {ValueName} is less than zero.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. Identifying Token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} must be non-negative");
            }

            return null;
        }

        /// <summary>
        /// Checks if the given integer is less than or equal to 0. Returns a BadRequest or the provided ActionResult if so.
        /// </summary>
        /// <param name="method">The name of the method calling this function, for logging purposes.</param>
        /// <param name="toCheck">The integer being checked for less than or equal to zero.</param>
        /// <param name="valueName">The name of the object being checked, for logging and error handling purposes.</param>
        /// <param name="token">An optional identifying token for logging. Uses the userId by default.</param>
        /// <param name="ret">An optional return function. Uses BadReqeust by default.</param>
        /// <returns>Null if <paramref name="toCheck"/> is not less than or equal to zero, or BadRequest or the given <paramref name="ret"/> otherwise.</returns>
        protected ActionResult? CheckForLessThanOrEqualTo0(string method, decimal? toCheck, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            if (toCheck <= 0)
            {
                _logger.LogWarning("{Method}: {ValueName} is less than or equal to zero.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. Identifying Token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} must be positive");
            }

            return null;
        }

        protected int GetUserId()
        {
            var user = User.FindFirst(ClaimTypes.NameIdentifier);
            return user == null ? 0 : int.Parse(user.Value);
        }

        protected static string SanitizeInput(string? input)
        {
            return input?.Replace(Environment.NewLine, "").Trim()!;
        }
    }
}