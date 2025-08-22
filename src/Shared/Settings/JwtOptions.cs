namespace Shared.Settings;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; }
    public int RefreshTokenExpirationMinutes { get; set; }
    public bool ExtendRefreshTokenEverytime { get; set; }

    public bool RevokeRefreshTokenAfterAbsoluteExpiration { get; set; }
    public int RefreshTokenAbsoluteExpirationMinutes { get; set; }
    public int RemoveExpiredRefreshTokenBeforeDays { get; set; }
    public bool SingleRefreshTokenPerUser { get; set; }
    public bool SingleLoginEnabled { get; set; }

    public int MaximumFailedAccessCount { get; set; }
    public int UserLockoutMinutes { get; set; }

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    public string TokenUrl { get; set; } = string.Empty;
    public string TokenRefreshUrl { get; set; } = string.Empty;
    public string UserInfoUrl { get; set; } = string.Empty;
}