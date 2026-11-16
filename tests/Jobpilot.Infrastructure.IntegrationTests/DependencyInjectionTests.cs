using Jobpilot.Application.Abstractions.JobSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch.JobSearchResponses;

namespace Jobpilot.Infrastructure.IntegrationTests;

/// <summary>Resolves the JobSearch client from DI, with the real resilience pipeline, against WireMock.</summary>
public sealed class DependencyInjectionTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private ServiceProvider? _provider;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static JobSearchQuery Query => JobSearchQuery.Create(["fg7B_yov_smw"], 10).Value;

    public void Dispose()
    {
        _provider?.Dispose();
        _server.Dispose();
    }

    private IJobSearchClient ResolveClient(string? baseUrl, TimeSpan? attemptTimeout = null, TimeSpan? totalTimeout = null)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["JobTech:JobSearch:BaseUrl"] = baseUrl,
            // Short retry delay keeps retry tests fast. Timeouts stay generous unless a test
            // is about timeouts: a cold first request can exceed a tight attempt timeout and be retried.
            ["JobTech:JobSearch:Resilience:Retry:Delay"] = "00:00:00.010",
            ["JobTech:JobSearch:Resilience:AttemptTimeout:Timeout"] = (attemptTimeout ?? TimeSpan.FromSeconds(5)).ToString("c"),
            ["JobTech:JobSearch:Resilience:TotalRequestTimeout:Timeout"] = (totalTimeout ?? TimeSpan.FromSeconds(10)).ToString("c"),
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        _provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return _provider.GetRequiredService<IJobSearchClient>();
    }

    [Fact]
    public async Task Client_UsesConfiguredBaseUrl()
    {
        _server.Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithBody(Body(Ad("parttime"))));

        var result = await ResolveClient(_server.Url).SearchAsync(Query, Ct);

        result.Value.Ads.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/relative")]
    public void Client_WithoutAbsoluteBaseUrl_FailsOnResolve(string? baseUrl) =>
        Should.Throw<OptionsValidationException>(() => ResolveClient(baseUrl));

    [Fact]
    public async Task Client_RetriesTransientFailure()
    {
        _server.Given(Request.Create().WithPath("/search").UsingGet())
            .InScenario("flaky").WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        _server.Given(Request.Create().WithPath("/search").UsingGet())
            .InScenario("flaky").WhenStateIs("recovered")
            .RespondWith(Response.Create().WithBody(Body(Ad("parttime"))));

        var result = await ResolveClient(_server.Url).SearchAsync(Query, Ct);

        result.Value.Ads.Count.ShouldBe(1);
        _server.LogEntries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Client_DoesNotRetryBadRequest()
    {
        _server.Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(400));

        var result = await ResolveClient(_server.Url).SearchAsync(Query, Ct);

        result.Error.Code.ShouldBe("JobSearch.Unavailable");
        _server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Client_TotalTimeout_FailsAsUnavailable()
    {
        _server.Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithBody(Body()).WithDelay(TimeSpan.FromSeconds(3)));
        var client = ResolveClient(_server.Url, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1));

        var result = await client.SearchAsync(Query, Ct);

        result.Error.Code.ShouldBe("JobSearch.Unavailable");
    }
}
