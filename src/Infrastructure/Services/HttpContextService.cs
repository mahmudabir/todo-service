using Domain.Abstractions.Services;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class HttpContextService(IHttpContextAccessor httpContextAccessor) : IHttpContextService
{
    public string GetAcceptLanguage()
    {
        return httpContextAccessor.HttpContext?.Request.Headers.AcceptLanguage.ToString() ?? "en";
    }

    public string? GetCurrentUserIdentity()
    {
        return httpContextAccessor.HttpContext.User.Identity?.Name;
    }
}