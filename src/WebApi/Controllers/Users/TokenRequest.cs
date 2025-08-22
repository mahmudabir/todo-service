using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Users.Authentications;

public class TokenRequest
{
    public string Username { get; set; }
    
    public string Password { get; set; }

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Scope { get; set; } = "apiScope";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? Authorization { get; set; }

    [RegularExpression("^(password|refresh_token)$", ErrorMessage = "Invalid grant type.")]
    [Required]
    [FromForm(Name = "grant_type")]
    public string? GrantType { get; set; } = "password";
}

public enum TokenGenerationType
{
    New = 0,
    Renew = 1,
    Revoke = 2
}