namespace Contracts.Dto
{
    public class LoginResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? JwtToken { get; set; } // Auth token for frontend
        public string? RefreshToken { get; set; } // NEW: Refresh token
        public UserDto? User { get; set; } // Optional: hydrated user info, simplifies frontend
    }
}