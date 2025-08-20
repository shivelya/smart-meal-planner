using Backend.DTOs;
using Backend.Model;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Services
{
    public interface IUserService
    {
        Task<User> GetByEmailAsync(string email);
        Task<User> GetByIdAsync(int id);
        Task<User> CreateUserAsync(string email, string password);
        Task<UserDto> UpdateUserDtoAsync(UserDto userDto);
        bool VerifyPasswordHash(string password, User user);
        Task ChangePasswordAsync(string userId, string oldPassword, string newPassword);
        Task<bool> UpdatePasswordAsync(int userId, string newPassword);
    }
}
