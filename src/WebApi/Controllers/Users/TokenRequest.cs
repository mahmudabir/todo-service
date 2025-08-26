using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers.Users;

public class TokenRequest
{
    public string Username { get; set; }
    
    public string Password { get; set; }

    [FromForm(Name = "refresh_token")]
    public string? RefreshToken { get; set; }

    public string? Scope { get; set; } = "apiScope";

    [FromForm(Name = "client_id")]
    public string? ClientId { get; set; }

    [FromForm(Name = "client_secret")]
    public string? ClientSecret { get; set; }

    [FromHeader]
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