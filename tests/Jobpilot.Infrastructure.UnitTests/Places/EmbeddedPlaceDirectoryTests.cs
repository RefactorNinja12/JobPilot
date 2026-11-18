using Jobpilot.Infrastructure.Places;

namespace Jobpilot.Infrastructure.UnitTests.Places;

public class EmbeddedPlaceDirectoryTests
{
    [Fact]
    public void All_LoadsTheEmbeddedSwedishPlaces()
    {
        var places = new EmbeddedPlaceDirectory().All;

        places.Count.ShouldBeGreaterThan(2_000);
        places.Select(p => p.Municipality).Distinct(StringComparer.Ordinal).Count().ShouldBeGreaterThan(280);
    }

    [Fact]
    public void All_HasLatitudeAndLongitudeTheRightWayRound()
    {
        var sundsvall = new EmbeddedPlaceDirectory().All.Single(p => p.Name == "Sundsvall");

        sundsvall.Municipality.ShouldBe("Sundsvall");
        sundsvall.Coordinates.Latitude.ShouldBe(62.39, tolerance: 0.01);
        sundsvall.Coordinates.Longitude.ShouldBe(17.31, tolerance: 0.01);
    }
}
