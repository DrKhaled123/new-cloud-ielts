namespace Contracts.Dto;

public class UserDto
{
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AuthId { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
}
