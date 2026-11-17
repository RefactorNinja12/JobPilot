using Jobpilot.Domain.Locations;

namespace Jobpilot.Domain.Jobs;

/// <summary>
/// Where the job is. Both parts are optional: unknown location is not an error,
/// and ads without coordinates must still pass the commute filter (flagged instead).
/// </summary>
public sealed record Workplace
{
    private Workplace(string? municipality, Coordinates? coordinates)
    {
        Municipality = municipality;
        Coordinates = coordinates;
    }

    public static Workplace Unknown { get; } = new(municipality: null, coordinates: null);

    public string? Municipality { get; }

    public Coordinates? Coordinates { get; }

    public bool HasCoordinates => Coordinates is not null;

    public static Workplace Create(string? municipality, Coordinates? coordinates)
    {
        var trimmed = string.IsNullOrWhiteSpace(municipality) ? null : municipality.Trim();
        return new Workplace(trimmed, coordinates);
    }
}
