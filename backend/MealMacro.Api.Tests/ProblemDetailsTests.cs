using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MealMacro.Api.Errors;
using MealMacro.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MealMacro.Api.Tests;

public class ProblemDetailsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static async Task<JsonElement> Body(HttpResponseMessage r) =>
        JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;

    private static void AssertBase(HttpResponseMessage r, JsonElement b, int status, string title, string detail, string instance)
    {
        Assert.Equal((HttpStatusCode)status, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Equal($"https://httpstatuses.com/{status}", b.GetProperty("type").GetString());
        Assert.Equal(title, b.GetProperty("title").GetString());
        Assert.Equal(status, b.GetProperty("status").GetInt32());
        Assert.Equal(detail, b.GetProperty("detail").GetString());
        Assert.Equal(instance, b.GetProperty("instance").GetString());
        Assert.Equal(r.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), b.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task NotFound_matches_contract()
    {
        var r = await _client.GetAsync("/api/foods/nope");
        var b = await Body(r);
        AssertBase(r, b, 404, "Not Found", "Resource not found", "/api/foods/nope");
        Assert.False(b.TryGetProperty("errors", out _));
    }

    [Fact]
    public async Task Validation_matches_contract_with_field_errors()
    {
        var r = await _client.PostAsJsonAsync("/api/targets/validate", new { Age = 5, WeightKg = 0 });
        var b = await Body(r);
        AssertBase(r, b, 400, "Validation Failed", "One or more validation errors occurred.", "/api/targets/validate");
        Assert.Equal(JsonValueKind.Array, b.GetProperty("errors").GetProperty("Age").ValueKind);
        Assert.Equal(JsonValueKind.Array, b.GetProperty("errors").GetProperty("WeightKg").ValueKind);
    }

    [Fact]
    public async Task Unhandled_exception_matches_contract_without_leaking_details()
    {
        using var f = factory.WithWebHostBuilder(b => b
            .ConfigureServices(s => s.AddExceptionHandler<GlobalExceptionHandler>().AddProblemDetails(o =>
                o.CustomizeProblemDetails = c => ProblemDetailsFactory.Apply(c.HttpContext, c.ProblemDetails)))
            .Configure(app =>
            {
                app.UseMiddleware<CorrelationIdMiddleware>();
                app.UseExceptionHandler();
                app.Run(_ => throw new InvalidOperationException("secret-internal-detail"));
            }));
        var r = await f.CreateClient().GetAsync("/boom");
        var text = await r.Content.ReadAsStringAsync();
        AssertBase(r, JsonDocument.Parse(text).RootElement, 500, "Internal Server Error",
            "An unexpected error occurred.", "/boom");
        Assert.DoesNotContain("secret-internal-detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("StackTrace", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_route_has_same_schema()
    {
        var r = await _client.GetAsync("/nope");
        var b = await Body(r);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://httpstatuses.com/404", b.GetProperty("type").GetString());
        Assert.Equal("Not Found", b.GetProperty("title").GetString());
        Assert.Equal("/nope", b.GetProperty("instance").GetString());
        Assert.True(b.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Inbound_correlation_id_is_reused_when_valid()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/foods/nope");
        req.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc-12345678");
        var r = await _client.SendAsync(req);
        Assert.Equal("abc-12345678", (await Body(r)).GetProperty("traceId").GetString());
        Assert.Equal("abc-12345678", r.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
    }

    [Fact]
    public async Task Missing_or_malformed_correlation_id_gets_generated_uuid()
    {
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/health");
        req.Headers.Add(CorrelationIdMiddleware.HeaderName, "bad id!");
        var id = (await _client.SendAsync(req)).Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single();
        Assert.True(Guid.TryParse(id, out _));
    }
}
