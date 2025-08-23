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

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        Result<ProblemDetails> problemDetailsResult = Result<ProblemDetails>.Error()
                                                                            .WithPayload(problemDetails)
                                                                            .WithMessage("Server failure");

        await httpContext.Response.WriteAsJsonAsync(problemDetailsResult, cancellationToken);

        return true;
    }
}