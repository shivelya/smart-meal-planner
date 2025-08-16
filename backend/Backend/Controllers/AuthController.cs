using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using SmartMealPlannerBackend.Model;
using SmartMealPlannerBackend.Services;

namespace SmartMealPlannerBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly PlannerContext _context;
        private readonly IJwtGenerator _jwt;

        public AuthController(PlannerContext context, IJwtGenerator jwt)
        {
            _context = context;
            _jwt = jwt;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest request)
        {
            // Check if user already exists
            if (_context.Users.Any(u => u.Email == request.Email))
                return BadRequest("User already exists.");

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                Email = request.Email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            var token = _jwt.GenerateToken(user);

            return Ok(new { Token = token });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            var user = _context.Users.SingleOrDefault(u => u.Email == request.Email);
            if (user == null) return Unauthorized("Invalid email or password.");

            // TODO: verify hash properly (BCrypt/Argon2, etc.)
            if (user.PasswordHash != request.Password) 
                return Unauthorized("Invalid email or password.");

            var token = _jwt.GenerateToken(user);
            return Ok(new { Token = token });
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
