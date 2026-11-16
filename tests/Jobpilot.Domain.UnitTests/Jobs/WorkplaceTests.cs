using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;

namespace Jobpilot.Domain.UnitTests.Jobs;

public class WorkplaceTests
{
    private static readonly Coordinates _sundbyberg = Coordinates.Create(59.3617, 17.9687).Value;

    [Fact]
    public void Create_WithMunicipalityAndCoordinates_KeepsBoth()
    {
        var workplace = Workplace.Create(" Sundbyberg ", _sundbyberg);

        workplace.Municipality.ShouldBe("Sundbyberg");
        workplace.Coordinates.ShouldBe(_sundbyberg);
        workplace.HasCoordinates.ShouldBeTrue();
    }

    [Fact]
    public void Create_WithoutCoordinates_IsValidButHasNoCoordinates()
    {
        var workplace = Workplace.Create("Sundbyberg", null);

        workplace.HasCoordinates.ShouldBeFalse();
        workplace.Municipality.ShouldBe("Sundbyberg");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankMunicipality_StoresNull(string? municipality) =>
        Workplace.Create(municipality, _sundbyberg).Municipality.ShouldBeNull();

    [Fact]
    public void Unknown_HasNothing()
    {
        Workplace.Unknown.Municipality.ShouldBeNull();
        Workplace.Unknown.HasCoordinates.ShouldBeFalse();
    }

    [Fact]
    public void Workplaces_WithSameValues_AreEqual() =>
        Workplace.Create("Sundbyberg", _sundbyberg).ShouldBe(Workplace.Create("Sundbyberg", _sundbyberg));
}
