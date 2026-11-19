using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

/// <summary>The real API in memory, with storage and the JobTech APIs faked. No AI is involved anywhere.</summary>
public sealed class JobEndpointsTests : IAsyncDisposable
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly FakeJobSearch _jobSearch = new();
    private readonly FakeEnrichments _enrichments = new();
    private readonly InMemoryAssessmentStore _assessments = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public JobEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProfileRepository>(_profiles);
            services.AddSingleton<IJobSearchClient>(_jobSearch);
            services.AddSingleton<IEnrichmentsClient>(_enrichments);
            services.AddSingleton<IAdCompetencyCache>(new InMemoryAdCompetencyCache());
            services.AddSingleton<IAssessmentStore>(_assessments);
        }));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    private void ProfileWithCSharpForBackendJobs()
    {
        var profile = Profile.Create();
        profile.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert, 8).Value);
        profile.SetTargetOccupations([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]);
        _profiles.Add(profile);
    }

    private static JobAd Ad(string id) =>
        JobAd.Create(id, "Utvecklare " + id, "Text", new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id),
            Employer.Create("Arbetsgivare", "2021005448").Value,
            Workplace.Unknown,
            Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
            WorkingHours.FullTime,
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateOnly(2026, 12, 1))).Value;

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

    private async Task AssessedAsync(string id, int chance) =>
        await _assessments.SaveAsync(
            AiAssessment.Create(id, chance, "Why " + id, new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.Zero)).Value,
            AdSnapshot.From(Ad(id)),
            Ct);

    [Fact]
    public async Task GetAssessed_ReturnsEveryAssessedAd_BestChanceFirst_WithoutSearchingJobTech()
    {
        await AssessedAsync("poor", 15);
        await AssessedAsync("good", 80);
        _jobSearch.Failure = Error.Unavailable("JobSearch.Down", "Should not be called.");

        var body = await _http.GetFromJsonAsync<JsonElement>("/api/jobs/assessed", Ct);

        body.EnumerateArray().Select(j => j.GetProperty("adId").GetString()).ShouldBe(["good", "poor"]);
        var good = body[0];
        good.GetProperty("headline").GetString().ShouldBe("Utvecklare good");
        good.GetProperty("employer").GetString().ShouldBe("Arbetsgivare");
        good.GetProperty("applicationDeadline").GetString().ShouldBe("2026-12-01");
        good.GetProperty("chance").GetInt32().ShouldBe(80);
        good.GetProperty("summary").GetString().ShouldBe("Why good");
    }

    [Fact]
    public async Task GetAssessed_NothingAssessed_IsAnEmptyList()
    {
        var body = await _http.GetFromJsonAsync<JsonElement>("/api/jobs/assessed", Ct);

        body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task GetJobs_ReturnsRankedMatchesAsCamelCaseJson()
    {
        ProfileWithCSharpForBackendJobs();
        _jobSearch.Ads.Add(Ad("java"));
        _jobSearch.Ads.Add(Ad("csharp"));
        _enrichments.SkillsByAd["java"] = ["Java"];
        _enrichments.SkillsByAd["csharp"] = ["C#", "Kafka"];

        var body = await _http.GetFromJsonAsync<JsonElement>("/api/jobs?top=1", Ct);

        body.GetProperty("fetched").GetInt32().ShouldBe(2);
        var match = body.GetProperty("matches").EnumerateArray().ShouldHaveSingleItem();
        match.GetProperty("adId").GetString().ShouldBe("csharp");
        match.GetProperty("fitScore").GetInt32().ShouldBe(50);
        match.GetProperty("missingSkills")[0].GetString().ShouldBe("Kafka");
    }

    [Fact]
    public async Task GetJobs_WithoutOccupations_Is400WithTheErrorCode()
    {
        using var response = await _http.GetAsync(new Uri("/api/jobs", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ProblemAsync(response)).GetProperty("code").GetString().ShouldBe("FindJobs.NoOccupations");
    }

    [Fact]
    public async Task GetJobs_TopOutOfRange_Is400()
    {
        ProfileWithCSharpForBackendJobs();

        using var response = await _http.GetAsync(new Uri("/api/jobs?top=51", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await ProblemAsync(response)).GetProperty("code").GetString().ShouldBe("FindJobs.TopOutOfRange");
    }

    [Fact]
    public async Task GetJobs_JobSearchDown_Is503()
    {
        ProfileWithCSharpForBackendJobs();
        _jobSearch.Failure = Error.Unavailable("JobSearch.Unavailable", "JobSearch could not be used: HTTP 503.");

        using var response = await _http.GetAsync(new Uri("/api/jobs", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await ProblemAsync(response)).GetProperty("code").GetString().ShouldBe("JobSearch.Unavailable");
    }

    [Fact]
    public async Task GetJobs_HasNoDailyLimit_UnlikeTheMcpTool()
    {
        ProfileWithCSharpForBackendJobs();

        for (var i = 0; i < 5; i++)
        {
            using var response = await _http.GetAsync(new Uri("/api/jobs", UriKind.Relative), Ct);
            response.StatusCode.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task GetJob_ReturnsTheAdInFull()
    {
        _jobSearch.Ads.Add(Ad("31744367"));

        var ad = await _http.GetFromJsonAsync<JsonElement>("/api/jobs/31744367", Ct);

        ad.GetProperty("adId").GetString().ShouldBe("31744367");
        ad.GetProperty("headline").GetString().ShouldBe("Utvecklare 31744367");
        ad.GetProperty("description").GetString().ShouldBe("Text");
    }

    [Fact]
    public async Task GetJob_Unknown_Is404WithTheErrorCode()
    {
        using var response = await _http.GetAsync(new Uri("/api/jobs/0000000", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await ProblemAsync(response)).GetProperty("code").GetString().ShouldBe("JobSearch.AdNotFound");
    }

    [Fact]
    public async Task Health_IsOk()
    {
        using var response = await _http.GetAsync(new Uri("/health", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
