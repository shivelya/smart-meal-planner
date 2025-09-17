namespace Backend.Services
{
    public interface IEmailService
    {
        /// <summary>
        /// Sends a password reset email to the specified recipient with the provided reset link.
        /// </summary>
        /// <param name="toEmail">The recipient's email address.</param>
        /// <param name="resetCode">The password reset code to include in the email.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task SendPasswordResetEmailAsync(string toEmail, string resetCode);
    }
}