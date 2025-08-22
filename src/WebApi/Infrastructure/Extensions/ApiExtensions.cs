namespace WebApi.Infrastructure.Extensions;

public static class ApiExtensions
{
    public static IServiceCollection AddApiControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
                {
                    // options.ModelValidatorProviders.Clear(); // Disable default asp.net core model state validation
                })
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = true;
                });

        return services;
    }

    public static IApplicationBuilder MapAppControllers(this WebApplication app)
    {
        app.MapControllers();

        return app;
    }
}