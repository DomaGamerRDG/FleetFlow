namespace backend.Dtos.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public string? ProfileImgUrl { get; set; }
    public ulong Id { get; set; }
}
