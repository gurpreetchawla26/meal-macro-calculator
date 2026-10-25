using System.Text.RegularExpressions;

namespace MealMacro.Api.Middleware;

/// <summary>
/// Resolves the traceId for each request: a well-formed inbound X-Correlation-Id is reused,
/// otherwise a new UUID is generated. It is set as the request's TraceIdentifier, echoed on the
/// response and attached to the logging scope.
/// </summary>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    public const string HeaderName = "X-Correlation-Id";

    [GeneratedRegex("^[A-Za-z0-9-]{8,64}$")]
    private static partial Regex ValidId();

    public async Task InvokeAsync(HttpContext context)
    {
        var inbound = context.Request.Headers[HeaderName].ToString();
        // Inbound values are untrusted (they reach logs and headers), so only accept a strict shape.
        var traceId = ValidId().IsMatch(inbound) ? inbound : Guid.NewGuid().ToString();

        context.TraceIdentifier = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = traceId;
            return Task.CompletedTask;
        });

        using (logger.BeginScope(new Dictionary<string, object> { ["traceId"] = traceId }))
        {
            await next(context);
        }
    }
}
