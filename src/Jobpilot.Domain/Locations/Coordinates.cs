using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Locations;

/// <summary>A point on Earth in decimal degrees (WGS84, as used by JobSearch and maps).</summary>
public sealed record Coordinates
{
    /// <summary>Mean Earth radius in km (IUGG).</summary>
    private const double EarthRadiusKm = 6371.0088;

    private Coordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static Result<Coordinates> Create(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || latitude is < -90 or > 90)
        {
            return CoordinatesErrors.LatitudeOutOfRange;
        }

        if (!double.IsFinite(longitude) || longitude is < -180 or > 180)
        {
            return CoordinatesErrors.LongitudeOutOfRange;
        }

        return new Coordinates(latitude, longitude);
    }

    /// <summary>
    /// Straight-line ("as the crow flies") distance in km, using the haversine formula.
    /// Accurate to well under 1% for commute distances.
    /// </summary>
    public double DistanceInKmTo(Coordinates other)
    {
        ArgumentNullException.ThrowIfNull(other);

        var deltaLatitude = ToRadians(other.Latitude - Latitude);
        var deltaLongitude = ToRadians(other.Longitude - Longitude);

        var a = (Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2))
            + (Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(other.Latitude))
                * Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2));

        // Min guards against rounding pushing the value just above 1 (NaN from Asin).
        return 2 * EarthRadiusKm * Math.Asin(Math.Min(1, Math.Sqrt(a)));
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180;
}
