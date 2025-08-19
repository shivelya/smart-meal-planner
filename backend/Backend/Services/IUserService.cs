using Backend.DTOs;
using Backend.Model;

namespace Backend.Services
{
    public interface IUserService
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByIdAsync(int id);
        Task<User> CreateUserAsync(string email, string password);
        Task<UserDto> UpdateUserDtoAsync(UserDto userDto);
        bool VerifyPasswordHash(string password, User user);
    }
}
