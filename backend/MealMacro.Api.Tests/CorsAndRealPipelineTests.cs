using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MealMacro.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace MealMacro.Api.Tests;

// Exercises the real Program.cs pipeline. The throwing endpoint exists in test code only.
public class CorsAndRealPipelineTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Origin = "http://localhost:5173";

    private sealed class BoomStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            var routes = (IEndpointRouteBuilder)app.Properties["__EndpointRouteBuilder"]!;
            routes.MapGet("/api/test-boom", (Func<string>)(() => throw new InvalidOperationException("secret-internal-detail")));
        };
    }

    private HttpClient Client(bool withBoom = false) =>
        (withBoom ? factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<IStartupFilter, BoomStartupFilter>())) : factory)
        .CreateClient();

    private static HttpRequestMessage Req(HttpMethod m, string url, string? origin = Origin, HttpContent? content = null)
    {
        var r = new HttpRequestMessage(m, url) { Content = content };
        if (origin != null) r.Headers.Add("Origin", origin);
        return r;
    }

    private static void AssertCors(HttpResponseMessage r)
    {
        Assert.Equal(Origin, r.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains("X-Correlation-Id", r.Headers.GetValues("Access-Control-Expose-Headers").Single());
    }

    [Fact]
    public async Task Cors_headers_present_on_404_resource()
    {
        var r = await Client().SendAsync(Req(HttpMethod.Get, "/api/foods/nope"));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        AssertCors(r);
    }

    [Fact]
    public async Task Cors_headers_present_on_404_unknown_route()
    {
        var r = await Client().SendAsync(Req(HttpMethod.Get, "/nope"));
        Assert.Equal(HttpStatusCode.NotFound, r.StatusCode);
        AssertCors(r);
    }

    [Fact]
    public async Task Cors_headers_present_on_400_validation()
    {
        var r = await Client().SendAsync(Req(HttpMethod.Post, "/api/targets/validate",
            content: JsonContent.Create(new { Age = 5, WeightKg = 0 })));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        AssertCors(r);
    }

    [Fact]
    public async Task Malformed_json_returns_400_bad_request_problem_details_with_cors()
    {
        var r = await Client().SendAsync(Req(HttpMethod.Post, "/api/targets/validate",
            content: new StringContent("{bad json", Encoding.UTF8, "application/json")));
        var b = JsonDocument.Parse(await r.Content.ReadAsStringAsync()).RootElement;
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://httpstatuses.com/400", b.GetProperty("type").GetString());
        Assert.Equal("Bad Request", b.GetProperty("title").GetString());
        Assert.Equal("/api/targets/validate", b.GetProperty("instance").GetString());
        Assert.Equal(r.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single(), b.GetProperty("traceId").GetString());
        AssertCors(r);
    }

    [Fact]
    public async Task Real_pipeline_500_is_generic_problem_details_with_cors()
    {
        var req = Req(HttpMethod.Get, "/api/test-boom");
        req.Headers.Add(CorrelationIdMiddleware.HeaderName, "abc-12345678");
        var r = await Client(withBoom: true).SendAsync(req);
        var text = await r.Content.ReadAsStringAsync();
        var b = JsonDocument.Parse(text).RootElement;
        Assert.Equal(HttpStatusCode.InternalServerError, r.StatusCode);
        Assert.Equal("application/problem+json", r.Content.Headers.ContentType?.MediaType);
        Assert.Equal("https://httpstatuses.com/500", b.GetProperty("type").GetString());
        Assert.Equal("An unexpected error occurred.", b.GetProperty("detail").GetString());
        Assert.Equal("abc-12345678", b.GetProperty("traceId").GetString());
        Assert.Equal("abc-12345678", r.Headers.GetValues(CorrelationIdMiddleware.HeaderName).Single());
        Assert.DoesNotContain("secret-internal-detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        AssertCors(r);
    }

    [Fact]
    public async Task Disallowed_origin_gets_no_cors_headers_on_error()
    {
        var r = await Client().SendAsync(Req(HttpMethod.Get, "/api/foods/nope", origin: "http://evil.example"));
        Assert.False(r.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
