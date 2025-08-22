using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Reflection;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebApi.Infrastructure.Extensions;

public static class TracingExtensions
{
    public static WebApplicationBuilder AddTracing(this WebApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .WithTracing(tracerProviderBuilder =>
            {
                tracerProviderBuilder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(builder.Environment.ApplicationName, serviceVersion: "1.0.0"))
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            // Filter out health checks and static files
                            var path = httpContext.Request.Path.Value;
                            return !path?.StartsWith("/health") == true &&
                                   !path?.StartsWith("/_") == true;
                        };
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        // Enable command text recording to see SQL queries
                        options.SetDbStatementForText = true;
                        options.SetDbStatementForStoredProcedure = true;
                        // options.RecordException = true;
                        // Optionally filter sensitive data
                        options.Filter = (activity, command) =>
                        {
                            // You can filter out sensitive operations here
                            return true;
                        };
                    })
                    .AddOtlpExporter(options =>
                    {
                        // Seq OTLP ingestion endpoint
                        options.Endpoint = new Uri(builder.Configuration.GetConnectionString("SeqOtlp")
                            ?? "http://localhost:5341/ingest/otlp/v1/traces");
                        options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;

                        // Add API key if configured
                        var seqApiKey = builder.Configuration["Seq:ApiKey"];
                        if (!string.IsNullOrEmpty(seqApiKey))
                        {
                            options.Headers = $"X-Seq-ApiKey={seqApiKey}";
                        }
                    });
            });

        return builder;
    }
}