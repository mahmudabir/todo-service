using System.Text.Json.Serialization;

namespace Shared.Models.Users;

public class TokenDetails
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }
}

public class TokenResponse : TokenDetails
{
    [JsonPropertyName("expires_in")]
    public double ExpiresIn { get; set; }

    [JsonPropertyName("refresh_expires_in")]
    public double RefreshExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }

    [JsonPropertyName("login")]
    public TokenDetails Login { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("roles")]
    public IEnumerable<string>? Roles { get; set; }
}