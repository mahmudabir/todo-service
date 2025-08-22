namespace Shared.Models.Users;

public class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? Username { get; set; }
}


public class ForceLogoutRequest
{
    public string Username { get; set; } = string.Empty;
    public string? Reason { get; set; }
}