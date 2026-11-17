namespace Jobpilot.Domain.Matching;

/// <summary>Why a dealbreaker rejected an ad.</summary>
public enum RejectionReason
{
    /// <summary>Full-time job, but the user does not accept full-time.</summary>
    FullTimeNotAccepted = 1,

    /// <summary>Part-time job, but the user does not accept part-time.</summary>
    PartTimeNotAccepted = 2,

    /// <summary>The workplace is farther from home than the commute limit.</summary>
    TooFarAway = 3,

    /// <summary>The employer is a staffing agency and the user excludes them.</summary>
    StaffingAgency = 4,
}
