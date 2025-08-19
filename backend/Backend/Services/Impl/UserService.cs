using Microsoft.EntityFrameworkCore;
using SmartMealPlannerBackend.DTOs;
using SmartMealPlannerBackend.Services;

namespace SmartMealPlannerBackend.Model
{
    public class UserSerivce: IUserService
    {
        private readonly PlannerContext _context;

        public UserSerivce(PlannerContext context)
        {
            _context = context;
        }

        public async Task<User> CreateUserAsync(string email, string password)
        {
            // Check if user already exists
            if (await GetByEmailAsync(email) != null)
                throw new InvalidOperationException("User already exists.");

            // Hash password
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                Email = email,
                PasswordHash = hashedPassword
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return user;
        }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email) ?? null!;
        }

        public async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == id) ?? null!;
        }

        public bool VerifyPasswordHash(string password, User user)
        {
            // Verify the password against the stored hash
            if (user == null || string.IsNullOrEmpty(user.PasswordHash))
                return false;

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }

        public Task<UserDto> UpdateUserDtoAsync(UserDto userDto)
        {
            throw new NotImplementedException();
        }
    }
}