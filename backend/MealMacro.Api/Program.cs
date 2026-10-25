using MealMacro.Api.Errors;
using MealMacro.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = ctx => ProblemDetailsFactory.Apply(ctx.HttpContext, ctx.ProblemDetails));

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

const string FrontendCors = "Frontend";
builder.Services.AddCors(o => o.AddPolicy(FrontendCors, p => p
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader().AllowAnyMethod()
    .WithExposedHeaders(CorrelationIdMiddleware.HeaderName)));

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages(); // problem+json for bodiless 4xx/5xx such as unmatched routes
app.UseCors(FrontendCors);

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

var foods = new Dictionary<string, string> { ["chicken-breast"] = "Chicken breast" };
app.MapGet("/api/foods/{id}", (string id) =>
    foods.TryGetValue(id, out var name)
        ? Results.Ok(new { id, name })
        : throw new NotFoundException("Food", id));

app.MapPost("/api/targets/validate", (TargetsRequest r) =>
{
    var errors = new Dictionary<string, string[]>();
    if (r.Age is < 15 or > 100) errors[nameof(r.Age)] = ["Age must be between 15 and 100."];
    if (r.WeightKg <= 0) errors[nameof(r.WeightKg)] = ["WeightKg must be greater than 0."];
    if (errors.Count > 0) throw new RequestValidationException(errors);
    return Results.Ok(new { valid = true });
});

app.Run();

public sealed record TargetsRequest(int Age, double WeightKg);

public partial class Program;

