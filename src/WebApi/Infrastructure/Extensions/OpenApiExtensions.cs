using WebApi.Infrastructure.OpenApiTransformers;

namespace WebApi.Infrastructure.Extensions;

public static class OpenApiExtensions
{
    public static IServiceCollection AddOpenApiAuthentication(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options => options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }

    public static WebApplication MapOpenApiConfig(this WebApplication app)
    {
        app.MapOpenApi();
        return app;
    }
}