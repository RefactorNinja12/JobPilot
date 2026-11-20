using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments.EnrichmentsResponses;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments;

/// <summary>Resolves the Enrichments client from DI, with the real resilience pipeline, against WireMock.</summary>
public sealed class EnrichmentsDependencyInjectionTests : IDisposable
{
    private const string Path = "/enrichtextdocuments";

    private readonly WireMockServer _server = WireMockServer.Start();
    private ServiceProvider? _provider;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static EnrichmentQuery Query
    {
        get
        {
            var details = new JobAdDetails(
                new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"),
                Employer.Create("Skatteverket", "2021005448").Value,
                Workplace.Unknown,
                Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
                WorkingHours.FullTime,
                new DateTimeOffset(2026, 11, 13, 13, 0, 0, TimeSpan.FromHours(1)),
                new DateOnly(2026, 11, 27));
            return EnrichmentQuery.Create([JobAd.Create("1", "Utvecklare", "C#", details).Value]).Value;
        }
    }

    public void Dispose()
    {
        _provider?.Dispose();
        _server.Dispose();
    }

    private IEnrichmentsClient ResolveClient(string? baseUrl, TimeSpan? attemptTimeout = null, TimeSpan? totalTimeout = null)
    {
        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["JobTech:Enrichments:BaseUrl"] = baseUrl,
            // Short retry delay keeps retry tests fast; timeouts stay generous unless a test is about them.
            ["JobTech:Enrichments:Resilience:Retry:Delay"] = "00:00:00.010",
            // The circuit breaker's window must be at least twice the attempt timeout.
            ["JobTech:Enrichments:Resilience:CircuitBreaker:SamplingDuration"] = "00:02:00",
            ["JobTech:Enrichments:Resilience:AttemptTimeout:Timeout"] = (attemptTimeout ?? TimeSpan.FromSeconds(30)).ToString("c"),
            ["JobTech:Enrichments:Resilience:TotalRequestTimeout:Timeout"] = (totalTimeout ?? TimeSpan.FromSeconds(60)).ToString("c"),
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        _provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return _provider.GetRequiredService<IEnrichmentsClient>();
    }

    [Fact]
    public async Task Client_UsesConfiguredBaseUrl()
    {
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .RespondWith(Response.Create().WithBody(Body(Real("1"))));

        var result = await ResolveClient(_server.Url).ExtractCompetenciesAsync(Query, Ct);

        result.Value.ShouldHaveSingleItem().Competencies.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Client_WithoutResilienceSection_UsesDefaults()
    {
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .RespondWith(Response.Create().WithBody(Body(Real("1"))));
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["JobTech:Enrichments:BaseUrl"] = _server.Url,
            })
            .Build();
        _provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();

        var result = await _provider.GetRequiredService<IEnrichmentsClient>().ExtractCompetenciesAsync(Query, Ct);

        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task VocabularyClient_UsesEnrichmentsBaseUrl()
    {
        _server.Given(Request.Create().WithPath("/synonymdictionary").UsingGet())
            .RespondWith(Response.Create().WithBody("""
                { "build_datetime": "2025-06-17T10:01:51", "items": [{ "concept": "C#", "term": "c#" }] }
                """));
        ResolveClient(_server.Url);

        var result = await _provider!.GetRequiredService<ISkillVocabularyClient>().GetCompetenciesAsync(Ct);

        result.Value.Find("c#")!.Name.ShouldBe("C#");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("/relative")]
    public void Client_WithoutAbsoluteBaseUrl_FailsOnResolve(string? baseUrl) =>
        Should.Throw<OptionsValidationException>(() => ResolveClient(baseUrl));

    [Fact]
    public async Task Client_RetriesTransientFailure()
    {
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .InScenario("flaky").WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .InScenario("flaky").WhenStateIs("recovered")
            .RespondWith(Response.Create().WithBody(Body(Real("1"))));

        var result = await ResolveClient(_server.Url).ExtractCompetenciesAsync(Query, Ct);

        result.IsSuccess.ShouldBeTrue();
        _server.LogEntries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Client_DoesNotRetryBadRequest()
    {
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .RespondWith(Response.Create().WithStatusCode(400));

        var result = await ResolveClient(_server.Url).ExtractCompetenciesAsync(Query, Ct);

        result.Error.Code.ShouldBe("Enrichments.Unavailable");
        _server.LogEntries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Client_TotalTimeout_FailsAsUnavailable()
    {
        _server.Given(Request.Create().WithPath(Path).UsingPost())
            .RespondWith(Response.Create().WithBody(Body(Real("1"))).WithDelay(TimeSpan.FromSeconds(3)));
        var client = ResolveClient(_server.Url, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(1));

        var result = await client.ExtractCompetenciesAsync(Query, Ct);

        result.Error.Code.ShouldBe("Enrichments.Unavailable");
    }
}
