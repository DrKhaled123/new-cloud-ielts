namespace Contracts.Dto
{
    public class SignUpResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UserDto? User { get; set; }
    }
}