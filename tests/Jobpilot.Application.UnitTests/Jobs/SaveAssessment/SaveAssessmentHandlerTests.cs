using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Jobs.SaveAssessment;
using Jobpilot.Application.Profiles;
using Jobpilot.Application.UnitTests.Jobs.FindJobs;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.SaveAssessment;

public class SaveAssessmentHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

    private static readonly string[] _matched = ["C#"];
    private static readonly string[] _missing = ["Kafka"];

    private readonly IAssessmentStore _store = Substitute.For<IAssessmentStore>();
    private readonly IJobSearchClient _jobSearch = Substitute.For<IJobSearchClient>();
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly InMemoryAdCompetencyCache _cache = new();
    private readonly Profile _profile = Profile.Create();
    private readonly SaveAssessmentHandler _handler;

    public SaveAssessmentHandlerTests()
    {
        _profile.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert).Value);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _handler = new SaveAssessmentHandler(_store, _jobSearch, _profiles, _cache, new FakeTimeProvider(_now));
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static JobAd Ad(string id) =>
        JobAd.Create(id, "Utvecklare", "Text", new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id),
            Employer.Create("Arbetsgivare", "2021005448").Value,
            Workplace.Create("Stockholm", coordinates: null),
            Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
            WorkingHours.FullTime,
            _now,
            new DateOnly(2026, 12, 1))).Value;

    [Fact]
    public async Task Save_StoresTheAssessment_StampedWithNow_AndASnapshotOfTheAd()
    {
        var ad = Ad("1");
        _jobSearch.GetAdAsync("1", Ct).Returns(ad);

        var result = await _handler.HandleAsync(new SaveAssessmentCommand(" 1 ", 70, " Strong C# match. "), Ct);

        result.Value.ShouldBe(new AssessmentView(70, "Strong C# match.", _now));
        await _store.Received(1).SaveAsync(
            Arg.Is<AiAssessment>(a => a.AdId == "1" && a.Chance == 70 && a.Summary == "Strong C# match." && a.AssessedAt == _now),
            AdSnapshot.From(ad),
            Ct);
    }

    [Fact]
    public async Task Save_WithCachedCompetencies_KeepsWhichSkillsMatchedAndWhichWereMissing()
    {
        _jobSearch.GetAdAsync("1", Ct).Returns(Ad("1"));
        _cache.Stored["1"] = [new ExtractedCompetency("C#", 0.9), new ExtractedCompetency("Kafka", 0.8)];

        await _handler.HandleAsync(new SaveAssessmentCommand("1", 70, "Ok."), Ct);

        await _store.Received(1).SaveAsync(
            Arg.Any<AiAssessment>(),
            Arg.Is<AdSnapshot>(s => s.MatchedSkills!.SequenceEqual(_matched) && s.MissingSkills!.SequenceEqual(_missing)),
            Ct);
    }

    [Fact]
    public async Task Save_WithoutCompetenciesOrProfile_LeavesTheSkillsUnknown()
    {
        _jobSearch.GetAdAsync("1", Ct).Returns(Ad("1"));
        _jobSearch.GetAdAsync("2", Ct).Returns(Ad("2"));
        _cache.Stored["2"] = [new ExtractedCompetency("C#", 0.9)];

        await _handler.HandleAsync(new SaveAssessmentCommand("1", 70, "Not analysed by find_jobs."), Ct);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);
        await _handler.HandleAsync(new SaveAssessmentCommand("2", 70, "No profile."), Ct);

        await _store.Received(2).SaveAsync(
            Arg.Any<AiAssessment>(),
            Arg.Is<AdSnapshot>(s => s.MatchedSkills == null && s.MissingSkills == null),
            Ct);
    }

    [Fact]
    public async Task Save_InvalidAssessment_FailsWithoutLookingUpOrStoring()
    {
        var result = await _handler.HandleAsync(new SaveAssessmentCommand("1", 101, "Ok."), Ct);

        result.Error.ShouldBe(AiAssessmentErrors.ChanceOutOfRange);
        await _jobSearch.DidNotReceiveWithAnyArgs().GetAdAsync(default!, TestContext.Current.CancellationToken);
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Save_AdCannotBeFetched_FailsWithoutStoring()
    {
        var notFound = Error.NotFound("JobSearch.AdNotFound", "No ad '1'.");
        _jobSearch.GetAdAsync("1", Ct).Returns(Result.Failure<JobAd>(notFound));

        var result = await _handler.HandleAsync(new SaveAssessmentCommand("1", 70, "Ok."), Ct);

        result.Error.ShouldBe(notFound);
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, TestContext.Current.CancellationToken);
    }
}
