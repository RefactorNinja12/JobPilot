using System.Globalization;
using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Employers;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Jobs.FindJobs;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.FindJobs;

public class FindJobsHandlerTests
{
    private static readonly DateTimeOffset _monday = new(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1));
    private static readonly Coordinates _stockholm = Coordinates.Create(59.3293, 18.0686).Value;
    private static readonly Coordinates _goteborg = Coordinates.Create(57.7089, 11.9746).Value;

    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly IJobSearchClient _jobSearch = Substitute.For<IJobSearchClient>();
    private readonly IEnrichmentsClient _enrichments = Substitute.For<IEnrichmentsClient>();
    private readonly IStaffingAgencyDirectory _agencies = Substitute.For<IStaffingAgencyDirectory>();
    private readonly InMemoryAdCompetencyCache _cache = new();
    private readonly IAssessmentStore _assessments = Substitute.For<IAssessmentStore>();
    private readonly Profile _profile = Profile.Create();
    private readonly Dictionary<string, (string Label, double Prediction)[]> _skillsByAd = new(StringComparer.Ordinal);
    private readonly FindJobsHandler _handler;

    public FindJobsHandlerTests()
    {
        _profile.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert).Value);
        _profile.SetTargetOccupations([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);

        // Enrichments answers from _skillsByAd for whichever ads it is asked about.
        _enrichments.ExtractCompetenciesAsync(Arg.Any<EnrichmentQuery>(), Arg.Any<CancellationToken>())
            .Returns(call => Result.Success<IReadOnlyList<EnrichedAd>>(
            [
                .. call.Arg<EnrichmentQuery>().Ads.Select(ad => new EnrichedAd(
                    ad.Id,
                    [.. _skillsByAd.GetValueOrDefault(ad.Id, []).Select(s => new ExtractedCompetency(s.Label, s.Prediction))])),
            ]));

        _assessments.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AiAssessment>(StringComparer.Ordinal));

        _handler = new FindJobsHandler(_profiles, _jobSearch, _enrichments, _cache, _assessments, _agencies);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static JobAd Ad(
        string id,
        WorkingHours hours = WorkingHours.FullTime,
        Coordinates? workplace = null,
        DateTimeOffset? published = null,
        string organizationNumber = "2021005448") =>
        JobAd.Create(id, "Utvecklare " + id, "Text", new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id),
            Employer.Create("Arbetsgivare " + id, organizationNumber).Value,
            Workplace.Create("Stockholm", workplace),
            Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
            hours,
            published ?? _monday,
            new DateOnly(2026, 12, 1))).Value;

    private void Found(params JobAd[] ads) => Found(skipped: 0, ads);

    private void Found(int skipped, params JobAd[] ads) =>
        _jobSearch.SearchAsync(Arg.Any<JobSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new JobSearchResult(ads, skipped));

    private void Requests(string adId, params (string Label, double Prediction)[] skills) => _skillsByAd[adId] = skills;

    private Task<Result<FindJobsResult>> FindAsync(int top = 25, int searchLimit = 200) =>
        _handler.HandleAsync(new FindJobsQuery(top, searchLimit), Ct);

    [Fact]
    public async Task Find_SearchesTheProfilesOccupations_WithTheLimit()
    {
        Found();

        await FindAsync(searchLimit: 150);

        await _jobSearch.Received(1).SearchAsync(
            Arg.Is<JobSearchQuery>(q => q.OccupationIds.Count == 1 && q.OccupationIds[0] == "7wdX_4rv_33z" && q.MaxResults == 150),
            Ct);
    }

    [Fact]
    public async Task Find_RanksByFitScore_ThenNewest_AndTakesTop()
    {
        Found(
            Ad("old-match", published: _monday.AddDays(-3)),
            Ad("no-match"),
            Ad("new-match", published: _monday));
        Requests("old-match", ("C#", 0.9));
        Requests("new-match", ("C#", 0.9));
        Requests("no-match", ("Java", 0.9));

        var result = (await FindAsync(top: 2)).Value;

        result.Matches.Select(m => m.AdId).ShouldBe(["new-match", "old-match"]);
        result.Matches[0].FitScore.ShouldBe(100);
        result.Matches[0].MatchedSkills.ShouldBe(["C#"]);
    }

    [Fact]
    public async Task Find_MapsTheSummary()
    {
        Found(Ad("1"));
        Requests("1", ("C#", 0.9), ("Kafka", 0.8));

        var match = (await FindAsync()).Value.Matches.ShouldHaveSingleItem();

        match.Headline.ShouldBe("Utvecklare 1");
        match.Employer.ShouldBe("Arbetsgivare 1");
        match.Municipality.ShouldBe("Stockholm");
        match.Occupation.ShouldBe("Backend-utvecklare");
        match.WorkingHours.ShouldBe("FullTime");
        match.ApplicationDeadline.ShouldBe(new DateOnly(2026, 12, 1));
        match.Url.ShouldBe(new Uri("https://arbetsformedlingen.se/platsbanken/annonser/1"));
        match.MissingSkills.ShouldBe(["Kafka"]);
    }

    [Fact]
    public async Task Find_RejectedAds_AreCountedPerReason_AndNeverEnriched()
    {
        _profile.UpdatePreferences(JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true,
            CommuteLimit.Create(_stockholm, 50).Value).Value);
        _agencies.IsStaffingAgency(Arg.Is<Employer>(e => e.OrganizationNumber == "5566778899")).Returns(returnThis: true);
        Found(
            Ad("ok", workplace: _stockholm),
            Ad("far", workplace: _goteborg),
            Ad("part-and-far", WorkingHours.PartTime, _goteborg),
            Ad("agency", workplace: _stockholm, organizationNumber: "5566778899"));

        var result = (await FindAsync()).Value;

        result.Matches.ShouldHaveSingleItem().AdId.ShouldBe("ok");
        result.Fetched.ShouldBe(4);
        result.Passed.ShouldBe(1);
        result.Rejected.ShouldBe(new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["TooFarAway"] = 2,
            ["PartTimeNotAccepted"] = 1,
            ["StaffingAgency"] = 1,
        });
        await _enrichments.Received(1).ExtractCompetenciesAsync(
            Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 1 && q.Ads[0].Id == "ok"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Find_AdWithUnknownDetails_PassesWithFlags()
    {
        _profile.UpdatePreferences(JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: true,
            excludeStaffingAgencies: false,
            CommuteLimit.Create(_stockholm, 50).Value).Value);
        Found(Ad("1", WorkingHours.Unknown, workplace: null));

        var match = (await FindAsync()).Value.Matches.ShouldHaveSingleItem();

        match.Unknowns.ShouldBe(["WorkingHours", "Location"]);
    }

    [Fact]
    public async Task Find_ManyAds_AreEnrichedInBatchesOfAtMost100()
    {
        Found([.. Enumerable.Range(0, 150).Select(i => Ad(i.ToString(CultureInfo.InvariantCulture)))]);

        var result = (await FindAsync(top: 50, searchLimit: 150)).Value;

        result.Passed.ShouldBe(150);
        result.Matches.Count.ShouldBe(50);
        await _enrichments.Received(1).ExtractCompetenciesAsync(Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 100), Arg.Any<CancellationToken>());
        await _enrichments.Received(1).ExtractCompetenciesAsync(Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 50), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Find_CachedAds_AreNotSentToEnrichmentsAgain()
    {
        Found(Ad("cached"), Ad("new"));
        _cache.Stored["cached"] = [new ExtractedCompetency("C#", 0.9)];
        Requests("new", ("Java", 0.9));

        var result = (await FindAsync()).Value;

        result.NewlyAnalysed.ShouldBe(1);
        result.Matches.Single(m => m.AdId == "cached").FitScore.ShouldBe(100);
        await _enrichments.Received(1).ExtractCompetenciesAsync(
            Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 1 && q.Ads[0].Id == "new"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Find_NewResults_AreCached_SoTheNextSearchSkipsEnrichments()
    {
        Found(Ad("1"));
        Requests("1", ("C#", 0.9));

        (await FindAsync()).Value.NewlyAnalysed.ShouldBe(1);
        var second = (await FindAsync()).Value;

        second.NewlyAnalysed.ShouldBe(0);
        second.Matches.ShouldHaveSingleItem().FitScore.ShouldBe(100);
        await _enrichments.ReceivedWithAnyArgs(1).ExtractCompetenciesAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Find_EnrichmentsFailsOnALaterBatch_KeepsTheEarlierBatchesCached()
    {
        Found([.. Enumerable.Range(0, 150).Select(i => Ad(i.ToString(CultureInfo.InvariantCulture)))]);
        var unavailable = Error.Unavailable("Enrichments.Unavailable", "Enrichments could not be used: timeout.");
        _enrichments.ExtractCompetenciesAsync(Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 50), Arg.Any<CancellationToken>())
            .Returns(unavailable);

        (await FindAsync(searchLimit: 150)).Error.ShouldBe(unavailable);

        _cache.Stored.Count.ShouldBe(100);
    }

    [Fact]
    public async Task Find_LeavesOutAdsClaudeHasAssessed_SoNewAdsTakeTheirPlaces_AndTheyCostNoEnrichments()
    {
        Found(Ad("assessed"), Ad("new"));
        _assessments.GetAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, AiAssessment>(StringComparer.Ordinal)
            {
                ["assessed"] = AiAssessment.Create("assessed", 70, "Strong C# match.", new DateTimeOffset(2026, 11, 17, 18, 0, 0, TimeSpan.Zero)).Value,
            });

        var result = (await FindAsync(top: 1)).Value;

        result.Matches.Select(m => m.AdId).ShouldBe(["new"]);
        result.Passed.ShouldBe(2);
        result.AlreadyAssessed.ShouldBe(1);
        result.NewlyAnalysed.ShouldBe(1);
        await _enrichments.Received(1).ExtractCompetenciesAsync(
            Arg.Is<EnrichmentQuery>(q => q.Ads.Count == 1 && q.Ads[0].Id == "new"),
            Ct);
    }

    [Fact]
    public async Task Find_NothingPasses_DoesNotCallEnrichments()
    {
        Found();

        var result = (await FindAsync()).Value;

        result.Matches.ShouldBeEmpty();
        await _enrichments.DidNotReceiveWithAnyArgs().ExtractCompetenciesAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Find_ReportsSkippedAds()
    {
        Found(skipped: 3, Ad("1"));

        (await FindAsync()).Value.Skipped.ShouldBe(3);
    }

    [Fact]
    public async Task Find_WithoutOccupations_FailsWithoutSearching()
    {
        _profile.SetTargetOccupations([]);

        (await FindAsync()).Error.ShouldBe(FindJobsErrors.NoOccupations);
        await _jobSearch.DidNotReceiveWithAnyArgs().SearchAsync(default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Find_WithoutProfile_FailsWithNoOccupations()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);

        (await FindAsync()).Error.ShouldBe(FindJobsErrors.NoOccupations);
    }

    [Theory]
    [InlineData(0, 200, "FindJobs.TopOutOfRange")]
    [InlineData(FindJobsHandler.MaxTop + 1, 200, "FindJobs.TopOutOfRange")]
    [InlineData(25, 0, "FindJobs.SearchLimitOutOfRange")]
    [InlineData(25, FindJobsHandler.MaxSearchLimit + 1, "FindJobs.SearchLimitOutOfRange")]
    public async Task Find_LimitsOutOfRange_FailAsValidation(int top, int searchLimit, string code)
    {
        var error = (await FindAsync(top, searchLimit)).Error;

        error.Code.ShouldBe(code);
        error.Type.ShouldBe(ErrorType.Validation);
    }

    [Fact]
    public async Task Find_JobSearchFailure_IsReturned()
    {
        var unavailable = Error.Unavailable("JobSearch.Unavailable", "JobSearch could not be used: HTTP 503.");
        _jobSearch.SearchAsync(Arg.Any<JobSearchQuery>(), Arg.Any<CancellationToken>()).Returns(unavailable);

        (await FindAsync()).Error.ShouldBe(unavailable);
    }

    [Fact]
    public async Task Find_EnrichmentsFailure_IsReturned()
    {
        Found(Ad("1"));
        var unavailable = Error.Unavailable("Enrichments.Unavailable", "Enrichments could not be used: timeout.");
        _enrichments.ExtractCompetenciesAsync(Arg.Any<EnrichmentQuery>(), Arg.Any<CancellationToken>()).Returns(unavailable);

        (await FindAsync()).Error.ShouldBe(unavailable);
    }
}
