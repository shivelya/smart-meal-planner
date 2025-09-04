namespace Backend.DTOs
{
    public class LoginRequest
    {
        public required string Email { get; set; }
        public required string Password { get; set; }
    }

    public class TokenResponse
    {
        public required string AccessToken { get; set; }
        public required string RefreshToken { get; set; }
    }

    public class ChangePasswordRequest
    {
        public required string OldPassword { get; set; }
        public required string NewPassword { get; set; }
    }

    public class ResetPasswordRequest
    {
        public required string ResetCode { get; set; }
        public required string NewPassword { get; set; }
    }

    public class LogoutRequest
    {
        public required string RefreshToken { get; set; }
    }
}