using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Abstractions.Usage;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Profiles;
using Jobpilot.Mcp.Limits;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>The daily cap on costly tools, through the real MCP server and client, with time under test control.</summary>
public sealed class DailyLimitTests : IAsyncLifetime
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly FakeJobSearch _jobSearch = new();

    // 2026-11-17 23:30 in Sweden (CET, UTC+1).
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 11, 17, 22, 30, 0, TimeSpan.Zero));

    private WebApplicationFactory<Program> _factory = null!;
    private McpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProfileRepository>(_profiles);
            services.AddSingleton<IJobSearchClient>(_jobSearch);
            services.AddSingleton<IEnrichmentsClient>(new FakeEnrichments());
            services.AddSingleton<IAdCompetencyCache>(new InMemoryAdCompetencyCache());
            services.AddSingleton<IAssessmentStore>(new InMemoryAssessmentStore());
            services.AddSingleton<IToolUsageCounter>(new InMemoryToolUsageCounter());
            services.AddSingleton<TimeProvider>(_time);
            services.Configure<DailyLimitOptions>(o => o.Tools["find_jobs"] = 1);
        }));

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(_factory.Server.BaseAddress, "mcp") },
            _factory.CreateClient(),
            loggerFactory: null,
            ownsHttpClient: true);
        _client = await McpClient.CreateAsync(transport, cancellationToken: Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private Task<CallToolResult> CallAsync(string tool) =>
        _client.CallToolAsync(tool, new Dictionary<string, object?>(StringComparer.Ordinal), cancellationToken: Ct).AsTask();

    private static string Text(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().ShouldHaveSingleItem().Text;

    private void GiveTheProfileAnOccupation()
    {
        var profile = Profile.Create();
        profile.SetTargetOccupations([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]);
        _profiles.Add(profile);
    }

    [Fact]
    public async Task LimitedTool_IsRefusedOnceTheDailyLimitIsUsed()
    {
        GiveTheProfileAnOccupation();

        var first = await CallAsync("find_jobs");
        var second = await CallAsync("find_jobs");

        first.IsError.ShouldNotBe(expected: true, customMessage: Text(first));
        second.IsError.ShouldBe(expected: true);
        Text(second).ShouldContain("DailyLimit.Reached: find_jobs has been used 1 of 1 times today");
    }

    [Fact]
    public async Task FailedCalls_DoNotUseUpTheLimit()
    {
        // No occupations yet: find_jobs fails with FindJobs.NoOccupations, twice.
        Text(await CallAsync("find_jobs")).ShouldContain("FindJobs.NoOccupations");
        Text(await CallAsync("find_jobs")).ShouldContain("FindJobs.NoOccupations");
        GiveTheProfileAnOccupation();

        (await CallAsync("find_jobs")).IsError.ShouldNotBe(expected: true);
    }

    [Fact]
    public async Task Limit_ResetsAtMidnightSwedishTime()
    {
        GiveTheProfileAnOccupation();
        await CallAsync("find_jobs");

        _time.Advance(TimeSpan.FromMinutes(31)); // 00:01 in Sweden, still 23:01 UTC

        (await CallAsync("find_jobs")).IsError.ShouldNotBe(expected: true);
    }

    [Fact]
    public async Task UnlimitedTools_AreNeverRefused()
    {
        for (var i = 0; i < 5; i++)
        {
            (await CallAsync("get_profile")).IsError.ShouldNotBe(expected: true);
        }
    }

    [Fact]
    public async Task DefaultLimits_CoverTheCostlyTools()
    {
        var defaults = new DailyLimitOptions().Tools;

        defaults["find_jobs"].ShouldBe(2);
        defaults["get_job_ad"].ShouldBe(10);
        defaults.ContainsKey("get_profile").ShouldBeFalse();
    }
}
