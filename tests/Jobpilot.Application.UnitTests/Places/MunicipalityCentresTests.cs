using Jobpilot.Application.Abstractions.Places;
using Jobpilot.Application.Places;
using Jobpilot.Domain.Locations;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Places;

public class MunicipalityCentresTests
{
    private static readonly Coordinates _town = Coordinates.Create(57.0, 12.0).Value;
    private static readonly Coordinates _village = Coordinates.Create(57.5, 12.5).Value;

    private static MunicipalityCentres CentresFor(params Place[] places)
    {
        var directory = Substitute.For<IPlaceDirectory>();
        directory.All.Returns(places);
        return new MunicipalityCentres(directory);
    }

    [Fact]
    public void Find_UsesTheLargestPlaceOfTheMunicipality()
    {
        var centres = CentresFor(new("Byn", "Kungsbacka", _village, 800), new("Kungsbacka", "Kungsbacka", _town, 20_000));

        centres.Find("Kungsbacka").ShouldBe(_town);
    }

    [Theory]
    [InlineData("kungsbacka")]
    [InlineData("  KUNGSBACKA ")]
    public void Find_IgnoresCaseAndSurroundingSpaces(string municipality) =>
        CentresFor(new Place("Kungsbacka", "Kungsbacka", _town, 20_000)).Find(municipality).ShouldBe(_town);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("Atlantis")]
    public void Find_MissingOrUnknownMunicipality_IsNull(string? municipality) =>
        CentresFor(new Place("Kungsbacka", "Kungsbacka", _town, 20_000)).Find(municipality).ShouldBeNull();
}
