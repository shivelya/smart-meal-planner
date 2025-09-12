using Backend.DTOs;

namespace Backend.Services
{
    /// <summary>
    /// Interface for category service.
    /// </summary>
    public interface ICategoryService
    {
        /// <summary>
        /// Retrieves all categories.
        /// </summary>
        /// <returns>An enumerable collection of category DTOs.</returns>
        Task<GetCategoriesResult> GetAllAsync();
    }
}