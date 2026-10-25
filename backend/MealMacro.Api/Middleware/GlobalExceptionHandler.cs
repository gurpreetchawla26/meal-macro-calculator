using MealMacro.Api.Errors;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace MealMacro.Api.Middleware;

/// <summary>Maps unhandled exceptions to RFC 7807 problem details without leaking internals.</summary>
public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            RequestValidationException v => ProblemDetailsFactory.CreateValidation(context, v.Errors),
            BadHttpRequestException => ProblemDetailsFactory.Create(
                context, StatusCodes.Status400BadRequest, "The request was malformed."),
            NotFoundException n => ProblemDetailsFactory.Create(context, StatusCodes.Status404NotFound, n.SafeMessage),
            _ => ProblemDetailsFactory.Create(
                context, StatusCodes.Status500InternalServerError, ProblemDetailsFactory.UnexpectedDetail),
        };

        // Full exception (with stack trace) stays server-side, tied to the traceId scope.
        if (problem.Status >= 500)
            logger.LogError(exception, "Unhandled exception. traceId={TraceId}", context.TraceIdentifier);
        else
            logger.LogWarning(exception, "Handled {Type}. traceId={TraceId}",
                exception.GetType().Name, context.TraceIdentifier);

        context.Response.StatusCode = problem.Status!.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
