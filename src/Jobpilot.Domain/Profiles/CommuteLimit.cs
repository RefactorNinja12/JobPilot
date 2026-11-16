using Jobpilot.Domain.Common;
using Jobpilot.Domain.Locations;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// Dealbreaker: the farthest a workplace may be from home, as a straight-line distance.
/// Home and max distance belong together, so a limit can never exist without a home location.
/// </summary>
public sealed record CommuteLimit
{
    public const int MaxDistanceLimitKm = 500;

    private CommuteLimit(Coordinates home, int maxDistanceKm)
    {
        Home = home;
        MaxDistanceKm = maxDistanceKm;
    }

    public Coordinates Home { get; }

    public int MaxDistanceKm { get; }

    public static Result<CommuteLimit> Create(Coordinates? home, int maxDistanceKm)
    {
        if (home is null)
        {
            return CommuteLimitErrors.HomeMissing;
        }

        if (maxDistanceKm is < 1 or > MaxDistanceLimitKm)
        {
            return CommuteLimitErrors.DistanceOutOfRange;
        }

        return new CommuteLimit(home, maxDistanceKm);
    }

    /// <summary>True when the workplace is at most <see cref="MaxDistanceKm"/> from home (inclusive).</summary>
    public bool IsWithinReach(Coordinates workplace)
    {
        ArgumentNullException.ThrowIfNull(workplace);
        return Home.DistanceInKmTo(workplace) <= MaxDistanceKm;
    }
}
