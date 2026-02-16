namespace backend.Dtos.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public string? ProfileImgUrl { get; set; }
}
