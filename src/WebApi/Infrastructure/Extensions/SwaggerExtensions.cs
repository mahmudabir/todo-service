using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

using Shared.Settings;

using Swashbuckle.AspNetCore.SwaggerUI;

namespace WebApi.Infrastructure.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSwaggerGen(c =>
        {
            // Configure Swagger to use OAuth2 with Resource Owner Password Credentials (ROPC) flow
            c.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Password = new OpenApiOAuthFlow
                    {
                        TokenUrl = new Uri($"{configuration["JwtSettings:TokenUrl"]}"),
                        RefreshUrl = new Uri($"{configuration["JwtSettings:TokenRefreshUrl"]}"),
                        Scopes = new Dictionary<string, string>
                        {
                            {
                                "apiScope", "Access your API"
                            },
                            {
                                "uiScope", "Access your UI"
                            }
                        }
                    }
                }
            });

            // Add security requirement to operations
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Id = "oauth2",
                            Type = ReferenceType.SecurityScheme
                        }
                    },
                    ["apiScope", "uiScope"]
                }
            });
        });

        return services;
    }

    public static WebApplication UseSwaggerUi(this WebApplication app)
    {
        var jwtOptions = app.Services.GetRequiredService<IOptions<JwtSettings>>().Value;
        
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint($"v1/swagger.json", $"API V1");

            c.OAuthClientId(jwtOptions.ClientId);
            c.OAuthClientSecret(jwtOptions.ClientSecret);
            c.OAuthAppName("Softoverse.CqrsKit");
            c.OAuthUseBasicAuthenticationWithAccessCodeGrant();

            c.EnablePersistAuthorization();
            c.EnableFilter();

            c.DisplayRequestDuration();
            c.DefaultModelRendering(ModelRendering.Model);
            c.DocExpansion(DocExpansion.List);
            c.EnableValidator();
            c.EnableTryItOutByDefault();
        });

        app.MapSwagger("{documentName}/swagger.json");

        return app;
    }
}