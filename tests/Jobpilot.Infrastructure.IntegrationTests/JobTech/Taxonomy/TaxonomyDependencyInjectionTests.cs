using Jobpilot.Application.Abstractions.Taxonomy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Taxonomy;

/// <summary>Resolves the occupation search from AddInfrastructure, with the real resilience pipeline.</summary>
public sealed class TaxonomyDependencyInjectionTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private ServiceProvider? _provider;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _provider?.Dispose();
        _server.Dispose();
    }

    private IOccupationSearch Resolve(string? baseUrl)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["JobTech:Taxonomy:BaseUrl"] = baseUrl,
                ["JobTech:Taxonomy:Resilience:Retry:Delay"] = "00:00:00.010",
            })
            .Build();
        _provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return _provider.GetRequiredService<IOccupationSearch>();
    }

    [Fact]
    public async Task Search_UsesConfiguredBaseUrl_AndRetriesTransientFailure()
    {
        _server.Given(Request.Create().WithPath("/v1/taxonomy/suggesters/autocomplete").UsingGet())
            .InScenario("flaky").WillSetStateTo("recovered")
            .RespondWith(Response.Create().WithStatusCode(503));
        _server.Given(Request.Create().WithPath("/v1/taxonomy/suggesters/autocomplete").UsingGet())
            .InScenario("flaky").WhenStateIs("recovered")
            .RespondWith(Response.Create().WithBody(
                """[{"taxonomy/id":"7wdX_4rv_33z","taxonomy/preferred-label":"Backend-utvecklare"}]"""));

        var result = await Resolve(_server.Url).SearchAsync("backend", 5, Ct);

        result.Value.ShouldHaveSingleItem().Label.ShouldBe("Backend-utvecklare");
        _server.LogEntries.Count.ShouldBe(2);
    }

    [Fact]
    public void Search_WithoutBaseUrl_FailsOnResolve() =>
        Should.Throw<OptionsValidationException>(() => Resolve(baseUrl: null));
}
