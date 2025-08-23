using Application;

using Shared;
using Shared.Settings;

using WebApi.Infrastructure.Extensions;
using WebApi.Infrastructure.Middlewares;

namespace WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        services.AddOpenApi();
        services.AddEndpointsApiExplorer();

        services.AddSwagger(configuration)
                .AddOpenApiAuthentication();

        services.AddApiControllers()
                .AddJsonOptions();

        services.AddAutoMapper(
            typeof(IApplicationMarker).Assembly,
            typeof(ISharedMarker).Assembly
        );

        // Shows UseCors with CorsPolicyBuilder.
        services.AddCors(options =>
                             options.AddPolicy("CorsPolicy", builder =>
                             {
                                 builder.AllowAnyOrigin()
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .SetIsOriginAllowed((host) => true)
                                        .AllowCredentials();

                                 builder.WithOrigins("*")
                                        .AllowAnyHeader()
                                        .AllowAnyMethod()
                                        .SetIsOriginAllowed((host) => true)
                                        .AllowCredentials();
                             })
                        );

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}