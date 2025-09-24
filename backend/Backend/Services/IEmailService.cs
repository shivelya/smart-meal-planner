using Backend.Model;

namespace Backend.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="user">The current user.</param>
        /// <param name="resetCode">The password reset code to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendPasswordResetEmailAsync(User user, string resetCode);
    }
}