namespace Contracts.Dto
{
    public class RefreshTokenResponseDto
    {
        public bool Success { get; set; }
        public string? JwtToken { get; set; }
        public string? RefreshToken { get; set; }
        public UserDto? User { get; set; }
        public string? Message { get; set; }
    }
}