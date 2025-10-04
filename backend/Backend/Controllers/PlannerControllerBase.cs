using System.ComponentModel.DataAnnotations;
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
                return ret != null ? ret() : StatusCode(500);
            }

            return null;
        }

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
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{Method}", method);
                _logger.LogInformation("Exiting {Method}: Identifying Token={Token}", method, token);
                return StatusCode(500);
            }
        }

        protected ActionResult? CheckForNull<T>(string method, T request, string? token = null, Func<ActionResult>? ret = null)
        {
            token ??= GetUserId().ToString();
            if (request == null)
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, token);
                return ret != null ? ret() : BadRequest("Request object is required.");
            }

            return null;
        }

        protected ActionResult? CheckForNullOrEmpty<T>(string method, IEnumerable<T> request, string? token = null, Func<ActionResult>? ret = null)
        {
            token ??= GetUserId().ToString();
            if (request == null || !request.Any())
            {
                _logger.LogWarning("{Method}: Request object is null.", method);
                _logger.LogInformation("{Method}: Exiting. token={Token}", method, token);
                return ret != null ? ret() : BadRequest("Request object is required.");
            }

            return null;
        }

        protected ActionResult? CheckForNullOrWhitespace(string method, string? value, string valueName, string? token = null, Func<ActionResult>? ret = null)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _logger.LogWarning("{Method}: {ValueName} is null or empty.", method, valueName);
                _logger.LogInformation("{Method}: Exiting. Identifying Token={Token}", method, token);
                return ret != null ? ret() : BadRequest($"{valueName} is required.");
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