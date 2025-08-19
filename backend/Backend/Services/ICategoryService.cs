using SmartMealPlannerBackend.DTOs;

namespace SmartMealPlannerBackend.Services
{
    /// <summary>
    /// Interface for category service.
    /// </summary>
    public interface ICategoryService
    {
        Task<IEnumerable<CategoryDto>> GetAllAsync();
    }
}