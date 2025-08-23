using Domain.Abstractions.Services;

using Microsoft.AspNetCore.Http;

namespace Infrastructure.Services;

public class HttpContextService(IHttpContextAccessor httpContextAccessor, IServiceProvider services) : IHttpContextService
{
    public string? GetCurrentUserIdentity()
    {
        return httpContextAccessor.HttpContext?.User.Identity?.Name;
    }
}