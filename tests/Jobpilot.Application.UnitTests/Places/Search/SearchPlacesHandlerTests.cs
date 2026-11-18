using Jobpilot.Application.Abstractions.Places;
using Jobpilot.Application.Places.Search;
using Jobpilot.Domain.Locations;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Places.Search;

public class SearchPlacesHandlerTests
{
    private readonly IPlaceDirectory _places = Substitute.For<IPlaceDirectory>();
    private readonly SearchPlacesHandler _handler;

    public SearchPlacesHandlerTests()
    {
        _places.All.Returns([
            Place("Sundsvall", "Sundsvall", 57_606),
            Place("Sundsbruk", "Sundsvall", 2_098),
            Place("Sund", "Gotland", 300),
            Place("Upplands Väsby", "Upplands Väsby", 36_534),
            Place("Kvissleby", "Sundsvall", 8_785),
        ]);
        _handler = new SearchPlacesHandler(_places);
    }

    [Fact]
    public void Search_ExactNameFirst_ThenPrefixMatchesByPopulation()
    {
        var names = _handler.Handle(new SearchPlacesQuery("SUND")).Value.Select(p => p.Name);

        names.ShouldBe(["Sund", "Sundsvall", "Sundsbruk"]);
    }

    [Fact]
    public void Search_MatchesALaterWordAfterPrefixMatches()
    {
        _handler.Handle(new SearchPlacesQuery("väsby")).Value.Select(p => p.Name).ShouldBe(["Upplands Väsby"]);
    }

    [Fact]
    public void Search_ReturnsMunicipalityAndCoordinates()
    {
        _handler.Handle(new SearchPlacesQuery(" kviss ")).Value
            .ShouldBe([new PlaceView("Kvissleby", "Sundsvall", 62.3, 17.4)]);
    }

    [Fact]
    public void Search_DoesNotMatchInsideAWord()
    {
        _handler.Handle(new SearchPlacesQuery("svall")).Value.ShouldBeEmpty();
    }

    [Fact]
    public void Search_IsCappedAtMaxResults()
    {
        _handler.Handle(new SearchPlacesQuery("sund", MaxResults: 2)).Value.Count.ShouldBe(2);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Search_BlankText_ReturnsNothing(string? text)
    {
        _handler.Handle(new SearchPlacesQuery(text)).Value.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SearchPlacesHandler.MaxResultsLimit + 1)]
    public void Search_MaxResultsOutOfRange_Fails(int maxResults)
    {
        _handler.Handle(new SearchPlacesQuery("sund", maxResults)).Error.ShouldBe(SearchPlacesErrors.MaxResultsOutOfRange);
    }

    private static Place Place(string name, string municipality, int population) =>
        new(name, municipality, Coordinates.Create(62.3, 17.4).Value, population);
}
