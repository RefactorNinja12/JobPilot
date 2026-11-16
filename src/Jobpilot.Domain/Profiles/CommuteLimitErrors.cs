using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

public static class CommuteLimitErrors
{
    public static readonly Error HomeMissing =
        Error.Validation("CommuteLimit.HomeMissing", "A home location is required to limit commute distance.");

    public static readonly Error DistanceOutOfRange =
        Error.Validation(
            "CommuteLimit.DistanceOutOfRange",
            $"Maximum commute distance must be between 1 and {CommuteLimit.MaxDistanceLimitKm} km.");
}
