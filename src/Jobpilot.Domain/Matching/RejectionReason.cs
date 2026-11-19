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

    /// <summary>The title says senior, lead, architect or manager, and the user excludes senior roles.</summary>
    TooSenior = 5,

    /// <summary>The description clearly asks for more years of experience than the user's maximum.</summary>
    TooMuchExperienceRequired = 6,
}
