using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Identity;

namespace Domain.Entities.Users;

public class ApplicationUser : IdentityUser
{
    public DateOnly? DateOfBirth { get; set; }

    [NotMapped]
    public IList<string>? Roles { get; set; }
    
    [JsonIgnore]
    public List<RefreshToken> RefreshTokens { get; set; }
}