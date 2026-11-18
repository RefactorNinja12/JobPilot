using Jobpilot.Application.Abstractions.Places;

namespace Jobpilot.Application.Places.Search;

/// <summary>A place match with WGS84 coordinates, ready to use as the commute home.</summary>
public sealed record PlaceView(string Name, string Municipality, double Latitude, double Longitude)
{
    public static PlaceView From(Place place)
    {
        ArgumentNullException.ThrowIfNull(place);
        return new PlaceView(place.Name, place.Municipality, place.Coordinates.Latitude, place.Coordinates.Longitude);
    }
}
