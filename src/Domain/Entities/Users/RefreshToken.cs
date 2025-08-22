using Microsoft.EntityFrameworkCore;

namespace Domain.Entities.Users;

[Owned]
public class RefreshToken
{
    public string Username { get; set; }
    public string Token { get; set; }
        
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        
    public DateTime CreatedAt { get; set; }
        
    public string? ReasonOfRevoke { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt != null && RevokedAt <= DateTime.UtcNow;
    
    public string? CreatedByIp { get; set; }
    public string? RevokedByIp { get; set; }
    public string? DeviceInfo { get; set; }
    
    public bool IsActive => !IsRevoked && !IsExpired;
}