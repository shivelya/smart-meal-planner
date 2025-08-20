using Microsoft.EntityFrameworkCore;
using Backend.DTOs;
using Backend.Services;

namespace Backend.Model
{
    public class UserSerivce: IUserService
    {
        private readonly PlannerContext _context;
        private readonly ILogger<UserSerivce> _logger;

        public UserSerivce(PlannerContext context, ILogger<UserSerivce> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<User> CreateUserAsync(string email, string password)
        {
            // Check if user already exists
            if (await GetByEmailAsync(email) != null)
            {
                _logger.LogInformation("Attempt to create a user that already exists: {Email}", email);
                throw new InvalidOperationException("User already exists.");
            }

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            _logger.LogDebug("Password hashed for user: {Email}; hash: {hash}", email, hashedPassword);

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User created successfully: {user}", user);

            return user;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email) ?? null!;
            if (user == null)
            {
                _logger.LogInformation("User not found with email: {Email}", email);
                return null!;
            }

            return user;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id) ?? null!;
            if (user == null)
            {
                _logger.LogInformation("User not found with ID: {Id}", id);
                return null!;
            }

            return user;
        }

        public bool VerifyPasswordHash(string password, User user)
        {
            // Verify the password against the stored hash
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            {
                _logger.LogWarning("User or password hash is null or empty.");
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public Task<UserDto> UpdateUserDtoAsync(UserDto userDto)
        {
            throw new NotImplementedException();
        }
    }
}