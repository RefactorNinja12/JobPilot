namespace Jobpilot.Domain.Matching;

/// <summary>
/// Something a dealbreaker could not check because the ad does not say. The ad still passes
/// ("unknown is not a failure"), and the flag tells the user or Claude to look.
/// </summary>
public enum UnknownDetail
{
    /// <summary>The ad does not say full-time or part-time.</summary>
    WorkingHours = 1,

    /// <summary>The ad has no coordinates, so the commute limit could not be checked.</summary>
    Location = 2,
}
