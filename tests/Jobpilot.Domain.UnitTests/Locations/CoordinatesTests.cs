using Jobpilot.Domain.Locations;

namespace Jobpilot.Domain.UnitTests.Locations;

public class CoordinatesTests
{
    private const double ToleranceKm = 2;

    private static readonly Coordinates _stockholm = Coordinates.Create(59.3293, 18.0686).Value;
    private static readonly Coordinates _uppsala = Coordinates.Create(59.8586, 17.6389).Value;
    private static readonly Coordinates _goteborg = Coordinates.Create(57.7089, 11.9746).Value;
    private static readonly Coordinates _malmo = Coordinates.Create(55.6050, 13.0038).Value;

    [Fact]
    public void Create_WithValidValues_Succeeds()
    {
        var result = Coordinates.Create(59.3293, 18.0686);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Latitude.ShouldBe(59.3293);
        result.Value.Longitude.ShouldBe(18.0686);
    }

    [Theory]
    [InlineData(-90, -180)]
    [InlineData(90, 180)]
    [InlineData(0, 0)]
    public void Create_AtBoundaries_Succeeds(double latitude, double longitude) =>
        Coordinates.Create(latitude, longitude).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(-90.0001)]
    [InlineData(90.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void Create_WithInvalidLatitude_FailsWithLatitudeOutOfRange(double latitude) =>
        Coordinates.Create(latitude, 0).Error.ShouldBe(CoordinatesErrors.LatitudeOutOfRange);

    [Theory]
    [InlineData(-180.0001)]
    [InlineData(180.0001)]
    [InlineData(double.NaN)]
    [InlineData(double.NegativeInfinity)]
    public void Create_WithInvalidLongitude_FailsWithLongitudeOutOfRange(double longitude) =>
        Coordinates.Create(0, longitude).Error.ShouldBe(CoordinatesErrors.LongitudeOutOfRange);

    [Fact]
    public void DistanceInKmTo_SamePoint_IsZero() =>
        _stockholm.DistanceInKmTo(_stockholm).ShouldBe(0, tolerance: 0.000001);

    [Fact]
    public void DistanceInKmTo_StockholmToUppsala_IsAbout64Km() =>
        _stockholm.DistanceInKmTo(_uppsala).ShouldBe(64, ToleranceKm);

    [Fact]
    public void DistanceInKmTo_StockholmToGoteborg_IsAbout398Km() =>
        _stockholm.DistanceInKmTo(_goteborg).ShouldBe(398, ToleranceKm);

    [Fact]
    public void DistanceInKmTo_StockholmToMalmo_IsAbout513Km() =>
        _stockholm.DistanceInKmTo(_malmo).ShouldBe(513, ToleranceKm);

    [Fact]
    public void DistanceInKmTo_IsSymmetric() =>
        _goteborg.DistanceInKmTo(_malmo).ShouldBe(_malmo.DistanceInKmTo(_goteborg), tolerance: 0.000001);

    [Fact]
    public void DistanceInKmTo_OppositeSidesOfEarth_IsHalfTheCircumference()
    {
        var a = Coordinates.Create(0, 0).Value;
        var b = Coordinates.Create(0, 180).Value;

        a.DistanceInKmTo(b).ShouldBe(Math.PI * 6371.0088, ToleranceKm);
    }

    [Fact]
    public void DistanceInKmTo_Null_Throws() =>
        Should.Throw<ArgumentNullException>(() => _stockholm.DistanceInKmTo(null!));

    [Fact]
    public void Coordinates_WithSameValues_AreEqual() =>
        Coordinates.Create(59.3293, 18.0686).Value.ShouldBe(_stockholm);
}
