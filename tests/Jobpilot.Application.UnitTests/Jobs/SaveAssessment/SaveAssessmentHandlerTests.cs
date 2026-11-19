using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Jobs.SaveAssessment;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.SaveAssessment;

public class SaveAssessmentHandlerTests
{
    private static readonly DateTimeOffset _now = new(2026, 11, 17, 18, 0, 0, TimeSpan.Zero);

    private readonly IAssessmentStore _store = Substitute.For<IAssessmentStore>();
    private readonly IJobSearchClient _jobSearch = Substitute.For<IJobSearchClient>();
    private readonly SaveAssessmentHandler _handler;

    public SaveAssessmentHandlerTests() => _handler = new SaveAssessmentHandler(_store, _jobSearch, new FakeTimeProvider(_now));

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
