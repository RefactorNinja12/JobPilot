using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Application.Occupations.Search;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Occupations.Search;

public class SearchOccupationsHandlerTests
{
    private readonly IOccupationSearch _search = Substitute.For<IOccupationSearch>();
    private readonly SearchOccupationsHandler _handler;

    public SearchOccupationsHandlerTests() => _handler = new SearchOccupationsHandler(_search);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Search_ReturnsIdsAndLabels()
    {
        _search.SearchAsync("backend", 5, Arg.Any<CancellationToken>())
            .Returns(Result.Success<IReadOnlyList<Occupation>>([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]));

        var result = await _handler.HandleAsync(new SearchOccupationsQuery("backend", 5), Ct);

        result.Value.ShouldBe([new OccupationView("7wdX_4rv_33z", "Backend-utvecklare")]);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SearchOccupationsHandler.MaxResultsLimit + 1)]
    public async Task Search_MaxResultsOutOfRange_FailsWithoutCalling(int maxResults)
    {
        (await _handler.HandleAsync(new SearchOccupationsQuery("backend", maxResults), Ct)).Error
            .ShouldBe(SearchOccupationsErrors.MaxResultsOutOfRange);

        await _search.DidNotReceiveWithAnyArgs().SearchAsync(default, default, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Search_ApiFailure_IsReturned()
    {
        var unavailable = Error.Unavailable("Taxonomy.Unavailable", "Taxonomy could not be used: HTTP 503.");
        _search.SearchAsync(Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(unavailable);

        (await _handler.HandleAsync(new SearchOccupationsQuery("backend"), Ct)).Error.ShouldBe(unavailable);
    }
}
