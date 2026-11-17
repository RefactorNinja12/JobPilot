using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Jobs.GetJobAd;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Jobs.GetJobAd;

public class GetJobAdHandlerTests
{
    private readonly IJobSearchClient _jobSearch = Substitute.For<IJobSearchClient>();
    private readonly GetJobAdHandler _handler;

    public GetJobAdHandlerTests() => _handler = new GetJobAdHandler(_jobSearch);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_ReturnsTheAdInFull()
    {
        var ad = JobAd.Create("31744367", "Pythonutvecklare", "Vi söker dig som kan Django.", new JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31744367"),
            Employer.Create("Exempel AB", organizationNumber: null).Value,
            Workplace.Create("Göteborg", coordinates: null),
            Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
            WorkingHours.FullTime,
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateOnly(2026, 12, 1))).Value;
        _jobSearch.GetAdAsync("31744367", Arg.Any<CancellationToken>()).Returns(ad);

        var view = (await _handler.HandleAsync(" 31744367 ", Ct)).Value;

        view.ShouldBe(new JobAdView(
            "31744367",
            "Pythonutvecklare",
            "Exempel AB",
            "Göteborg",
            "Systemutvecklare/Programmerare",
            "FullTime",
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateOnly(2026, 12, 1),
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31744367"),
            "Vi söker dig som kan Django."));
    }

    [Fact]
    public async Task Get_Missing_PassesTheErrorThrough()
    {
        var notFound = Error.NotFound("JobSearch.AdNotFound", "JobSearch has no ad '1'.");
        _jobSearch.GetAdAsync("1", Arg.Any<CancellationToken>()).Returns(notFound);

        (await _handler.HandleAsync("1", Ct)).Error.ShouldBe(notFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public async Task Get_BlankId_FailsWithoutCalling(string? adId)
    {
        (await _handler.HandleAsync(adId, Ct)).Error.ShouldBe(GetJobAdErrors.AdIdEmpty);

        await _jobSearch.DidNotReceiveWithAnyArgs().GetAdAsync(default!, TestContext.Current.CancellationToken);
    }
}
