namespace MealMacro.Api.Errors;

/// <summary>Resource lookup failed; mapped to 404. SafeMessage is the only text shown to clients.</summary>
public sealed class NotFoundException(string resourceName, string? resourceId = null, string safeMessage = "Resource not found")
    : Exception($"{resourceName} '{resourceId}' not found")
{
    public string ResourceName { get; } = resourceName;
    public string? ResourceId { get; } = resourceId;
    public string SafeMessage { get; } = safeMessage;
}

/// <summary>Request failed validation; mapped to 400 with per-field errors.</summary>
public sealed class RequestValidationException(IDictionary<string, string[]> errors)
    : Exception(ProblemDetailsFactory.ValidationDetail)
{
    public IDictionary<string, string[]> Errors { get; } = errors;
}
