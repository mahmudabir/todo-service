using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

using Shared;
using Shared.Constants;

namespace Infrastructure.Authentication;

public class CustomAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        AuthorizationPolicy? authorizationPolicy = policyName.Trim() switch
        {
            Auth.ApiKeyPolicy => new AuthorizationPolicyBuilder().AddRequirements(new ApiKeyRequirement()).Build(),
            Auth.ApiKeyAndJwtPolicy => new AuthorizationPolicyBuilder().RequireAuthenticatedUser().AddRequirements(new ApiKeyRequirement()).Build(),
            null or "" => await base.GetPolicyAsync(policyName),
            _ => await base.GetPolicyAsync(policyName)
        };

        return authorizationPolicy;
    }
}

public class ApiKeyRequirement : IAuthorizationRequirement
{
}

public class ApiKeyHandler(IHttpContextAccessor httpContextAccessor, IConfiguration configuration) : AuthorizationHandler<ApiKeyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ApiKeyRequirement requirement)
    {
// #if DEBUG
//         return Task.CompletedTask;
// #endif

        string? apiKey = httpContextAccessor?.HttpContext?.Request.Headers[Auth.ApiKeyHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Fail();
            return Task.CompletedTask;
        }

        if (!apiKey.Equals(configuration[Auth.ApiKeyPolicy]))
        {
            context.Fail();
            return Task.CompletedTask;
        }
        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

public class ApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
// #if DEBUG
//         return Task.CompletedTask;
// #endif

        string? apiKey = context.HttpContext?.Request.Headers[Auth.ApiKeyHeaderName].ToString();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Results.BadRequest();
        }

        if (!apiKey.Equals(configuration[Auth.ApiKeyPolicy]))
        {
            return Results.Unauthorized();
        }
        return await next(context);
    }
}