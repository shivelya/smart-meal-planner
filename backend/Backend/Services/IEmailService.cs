namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string to, string resetLink);
    }
}