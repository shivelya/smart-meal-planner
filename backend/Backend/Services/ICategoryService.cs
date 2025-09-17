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
        /// <param name="ct">Cancellation token</param>
        /// <returns>An enumerable collection of category DTOs.</returns>
        Task<GetCategoriesResult> GetAllAsync(CancellationToken ct);
    }
}