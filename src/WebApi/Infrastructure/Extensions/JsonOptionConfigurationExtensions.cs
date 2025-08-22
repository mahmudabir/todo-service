using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Http.Json;

namespace WebApi.Infrastructure.Extensions;

public static class JsonOptionConfigurationExtensions
{
    public static void AddJsonOptions(this IServiceCollection services)
    {
        IList<JsonConverter> jsonConverters = [
            new JsonStringEnumConverter(),
        ];

        Action<JsonOptions> jsonConfigureOptions = (options) =>
        {
            options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.WriteIndented = false;
            options.SerializerOptions.Encoder = JavaScriptEncoder.Default;
            options.SerializerOptions.AllowTrailingCommas = true;
            // options.SerializerOptions.MaxDepth = 32;
            options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

            foreach (var jsonConverter in jsonConverters)
            {
                options.SerializerOptions.Converters.Add(jsonConverter);
            }
        };

        services.AddControllers().AddJsonOptions((options) =>
        {
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Default;
            options.JsonSerializerOptions.AllowTrailingCommas = true;
            // options.JsonSerializerOptions.MaxDepth = 32;
            options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

            foreach (var jsonConverter in jsonConverters)
            {
                options.JsonSerializerOptions.Converters.Add(jsonConverter);
            }
        });

        // For System.Text.Json
        services.Configure(jsonConfigureOptions);

        // For System.Text.Json
        services.ConfigureHttpJsonOptions(jsonConfigureOptions);
    }
}