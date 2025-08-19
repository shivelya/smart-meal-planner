using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Backend.Model;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;

        public AuthController(ITokenService jwt, IUserService userService)
        {
            _tokenService = jwt;
            _userService = userService;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            if (request.Email == null || request.Password == null)
                return BadRequest("Email and password are required.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Check if user already exists
            if (await _userService.GetByEmailAsync(request.Email) != null)
                return BadRequest("User already exists.");

            var user = await _userService.CreateUserAsync(request.Email, request.Password);
            if (user == null)
                return StatusCode(500, "Failed to create user.");

            var result = await GenerateTokens(user);
            if (result.Error != null)
            {
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");
            }

            // Return the tokens
            return Ok(new TokenResponse
            {
                 AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken?.Token!,
            });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            if (request.Email == null || request.Password == null)
                return BadRequest("Email and password are required.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userService.GetByEmailAsync(request.Email);
            if (user == null) return Unauthorized("Invalid email or password.");

            if (!_userService.VerifyPasswordHash(request.Password, user))
                return Unauthorized("Invalid email or password.");

            var result = await GenerateTokens(user);
            if (result.Error != null)
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken?.Token!,
            });
        }

        // POST: api/auth/refresh
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody]string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Refresh token is required.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var oldRefreshToken = await _tokenService.FindRefreshToken(refreshToken);

            // if the refresh token is not found or is expired/revoked, return Unauthorized
            // this is a security measure to prevent token reuse
            if (oldRefreshToken == null || oldRefreshToken.Expires < DateTime.UtcNow
                || oldRefreshToken.IsRevoked)
                return Unauthorized("Invalid refresh token.");

            var user = await _userService.GetByIdAsync(oldRefreshToken.UserId);
            if (user == null) return Unauthorized("Invalid user.");

            // Mark the old refresh token as revoked
            oldRefreshToken.IsRevoked = true;

            // Generate new tokens
            var result = await GenerateTokens(user);
            if (result.Error != null)
                return result.Error ?? StatusCode(500, "Failed to generate tokens.");

            return Ok(new TokenResponse
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken.Token!
            });
        }

        // POST: api/auth/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody]string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                return BadRequest("Refresh token is required.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var refreshTokenObj = await _tokenService.FindRefreshToken(refreshToken);

            // If the refresh token is not found, we can still return OK
            // This is to ensure that the client can safely call logout without worrying about the token's existence
            // This is a common practice to avoid leaking information about token validity
            if (refreshTokenObj == null)
                return Ok();

            await _tokenService.RevokeRefreshToken(refreshTokenObj);
            return Ok();
        }

        private async Task<TokenGenerateResult> GenerateTokens(User user)
        {
            var result = new TokenGenerateResult();
            try
            {
                var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                result.AccessToken = _tokenService.GenerateAccessToken(user);
                result.RefreshToken = await _tokenService.GenerateRefreshToken(user, ip);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating token: {ex.Message}");
                result.Error = StatusCode(500, $"Internal server error: Failed to generate token.");
                return result;
            }

            if (result.AccessToken == null || result.RefreshToken == null)
            {
                result.Error = StatusCode(500, "Failed to generate token.");
                return result;
            }

            return result;
        }
    }

    public class TokenGenerateResult
    {
        public IActionResult? Error { get; set; }
        public string AccessToken { get; set; } = null!;
        public RefreshToken RefreshToken { get; set; } = null!;
    }

    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class TokenResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }
}