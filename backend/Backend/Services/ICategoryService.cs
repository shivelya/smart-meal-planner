using Backend.DTOs;

namespace Backend.Services
{
    /// <summary>
    /// Interface for category service.
    /// </summary>
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
    }
}