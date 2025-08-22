using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

using Shared.Constants;
using Shared.Models.Results;

using Environment = System.Environment;

namespace WebApi.Infrastructure.Middlewares;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext,
                                                Exception exception,
                                                CancellationToken cancellationToken)
    {
        logger.LogError(exception, LogMessages.Unexpected);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
            Title = "Server failure"
        };

#if DEBUG
        problemDetails.Title = exception.Message;
        problemDetails.Instance = Truncate(exception.StackTrace?.Replace(Environment.NewLine, "\\n"), 5000);
        problemDetails.Detail = Truncate(exception.StackTrace?.Replace(Environment.NewLine, "\\n"), 5000);

        IDictionary<string, object?> problemDetailsExtensions = new Dictionary<string, object?>();

        // Ensure that the request body can be read
        if (httpContext.Request.ContentLength > 0 && httpContext.Request.Body.CanSeek)
        {
            problemDetailsExtensions["requestBody"] = await GetRequestBody(httpContext);
        }

        problemDetailsExtensions["requestForm"] = Truncate(GetRequestForm(httpContext), 2000);
        problemDetailsExtensions["queryParams"] = Truncate(string.Join(", ", httpContext.Request.Query.Select(x => $"{x.Key}={x.Value}")), 2000);
        problemDetailsExtensions["url"] = httpContext.Request.Path.ToString();
        problemDetailsExtensions["pathParams"] = Truncate(string.Join(", ", httpContext.Request.RouteValues.Select(x => $"{x.Key}={x.Value}")), 2000);
        problemDetailsExtensions["requestHeaders"] = Truncate(GetRequestHeaders(httpContext), 5000);

        async Task<string> GetRequestBody(HttpContext httpContext)
        {
            httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(httpContext.Request.Body);
            return Truncate(await reader.ReadToEndAsync(cancellationToken), 5000);
        }

        static string GetRequestForm(HttpContext httpContext)
        {
            return Truncate(string.Join(", ", httpContext.Request.Form.Select(x => $"{x.Key}={x.Value}")), 2000);
        }

        static string GetRequestHeaders(HttpContext httpContext)
        {
            return Truncate(string.Join(", ", httpContext.Request.Headers.Select(x => $"{x.Key}={x.Value}")), 5000);
        }

        static string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return value.Length > maxLength ? value.Substring(0, maxLength) + "..." : value;
        }
#endif

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        Result<ProblemDetails> problemDetailsResult = Result<ProblemDetails>.Error()
                                                                            .WithPayload(problemDetails)
                                                                            .WithMessage("Server failure");

        await httpContext.Response.WriteAsJsonAsync(problemDetailsResult, cancellationToken);

        return true;
    }
}