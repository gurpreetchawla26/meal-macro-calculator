using Microsoft.AspNetCore.Mvc;

namespace MealMacro.Api.Errors;

/// <summary>Single place that shapes every RFC 7807 payload (type, title, instance, traceId).</summary>
public static class ProblemDetailsFactory
{
    public const string ValidationDetail = "One or more validation errors occurred.";
    public const string UnexpectedDetail = "An unexpected error occurred.";

    public static string TypeFor(int status) => $"https://httpstatuses.com/{status}";

    public static string TitleFor(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status500InternalServerError => "Internal Server Error",
        _ => ReasonPhrase(status),
    };

    private static string ReasonPhrase(int status) =>
        Microsoft.AspNetCore.WebUtilities.ReasonPhrases.GetReasonPhrase(status) is { Length: > 0 } p ? p : "Error";

    public static ProblemDetails Create(HttpContext context, int status, string detail, string? title = null) =>
        Apply(context, new ProblemDetails { Status = status, Title = title, Detail = detail });

    public static ProblemDetails CreateValidation(HttpContext context, IDictionary<string, string[]> errors) =>
        Apply(context, new Microsoft.AspNetCore.Http.HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Detail = ValidationDetail,
        });

    /// <summary>Fills any missing standard fields; also used for bodiless 4xx/5xx via ProblemDetailsService.</summary>
    public static ProblemDetails Apply(HttpContext context, ProblemDetails problem)
    {
        var status = problem.Status ?? context.Response.StatusCode;
        problem.Status = status;
        problem.Type = TypeFor(status);
        problem.Title ??= TitleFor(status);
        problem.Instance ??= context.Request.Path;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        return problem;
    }
}
