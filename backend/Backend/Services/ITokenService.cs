using SmartMealPlannerBackend.Model;

namespace SmartMealPlannerBackend.Services
{
    /// <summary>
    /// Interface for JWT token generation.
    /// </summary>
    public interface IJwtGenerator
    {
        string GenerateToken(User user);
    }
}