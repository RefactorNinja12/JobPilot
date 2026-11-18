using System.Net;
using Jobpilot.Application.Profiles;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class WebUiTests : IAsyncDisposable
{
    private readonly string _webRoot = Directory.CreateTempSubdirectory("jobpilot-wwwroot-").FullName;
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public WebUiTests()
    {
        File.WriteAllText(Path.Combine(_webRoot, "index.html"), "<!doctype html><title>Jobpilot</title>");
        Directory.CreateDirectory(Path.Combine(_webRoot, "assets"));
        File.WriteAllText(Path.Combine(_webRoot, "assets", "index-abc123.js"), "console.log('jobpilot')");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host
            .UseWebRoot(_webRoot)
            .ConfigureTestServices(services => services.AddSingleton<IProfileRepository>(new InMemoryProfileRepository())));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
        Directory.Delete(_webRoot, recursive: true);
    }

    private Task<HttpResponseMessage> GetAsync(string path) => _http.GetAsync(new Uri(path, UriKind.Relative), Ct);

    [Theory]
    [InlineData("/")]
    [InlineData("/profile")]
    public async Task Pages_ServeIndexHtml_Uncached(string path)
    {
        using var response = await GetAsync(path);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("<title>Jobpilot</title>");
        response.Headers.CacheControl.ShouldNotBeNull().NoCache.ShouldBeTrue();
    }

    [Fact]
    public async Task Assets_AreServed()
    {
        using var response = await GetAsync("/assets/index-abc123.js");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldContain("jobpilot");
    }

    [Fact]
    public async Task UnknownApiPath_Is404_NotThePage()
    {
        using var response = await GetAsync("/api/nothing-here");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync(Ct)).ShouldNotContain("<title>");
    }

    [Fact]
    public async Task Api_StillAnswers()
    {
        using var response = await GetAsync("/api/profile");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType.ShouldNotBeNull().MediaType.ShouldBe("application/json");
    }
}
