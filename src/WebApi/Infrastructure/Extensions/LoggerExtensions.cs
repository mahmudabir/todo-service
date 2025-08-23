using Serilog;
using Serilog.Enrichers.Span;

namespace WebApi.Infrastructure.Extensions;

public static class LoggerExtensions
{
    public static WebApplicationBuilder AddLogger(this WebApplicationBuilder builder)
    {
        // builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
        builder.Host.UseSerilog((context, loggerConfig) =>
                                    loggerConfig.ReadFrom.Configuration(context.Configuration)
                                                .Enrich.WithSpan());

        return builder;
    }

    public static IApplicationBuilder UseLogger(this IApplicationBuilder app)
    {
        app.UseSerilogRequestLogging();

        return app;
    }
}