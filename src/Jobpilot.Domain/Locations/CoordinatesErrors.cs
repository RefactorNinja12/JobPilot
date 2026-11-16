using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Locations;

public static class CoordinatesErrors
{
    public static readonly Error LatitudeOutOfRange =
        Error.Validation("Coordinates.LatitudeOutOfRange", "Latitude must be a number between -90 and 90.");

    public static readonly Error LongitudeOutOfRange =
        Error.Validation("Coordinates.LongitudeOutOfRange", "Longitude must be a number between -180 and 180.");
}
