using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// Dealbreakers: hard requirements a job ad must meet to be shown at all.
/// Applied in code before any AI step, and the AI can never override them.
/// </summary>
public sealed record JobPreferences
{
    private JobPreferences(
        bool acceptsFullTime,
        bool acceptsPartTime,
        bool excludeStaffingAgencies,
        CommuteLimit? commuteLimit)
    {
        AcceptsFullTime = acceptsFullTime;
        AcceptsPartTime = acceptsPartTime;
        ExcludeStaffingAgencies = excludeStaffingAgencies;
        CommuteLimit = commuteLimit;
    }

    /// <summary>No requirements: every ad passes. Default for new profiles.</summary>
    public static JobPreferences None { get; } =
        new(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, commuteLimit: null);

    public bool AcceptsFullTime { get; }

    public bool AcceptsPartTime { get; }

    public bool ExcludeStaffingAgencies { get; }

    /// <summary>Maximum commute distance from home, or null for no requirement.</summary>
    public CommuteLimit? CommuteLimit { get; }

    public static Result<JobPreferences> Create(
        bool acceptsFullTime,
        bool acceptsPartTime,
        bool excludeStaffingAgencies,
        CommuteLimit? commuteLimit = null)
    {
        if (!acceptsFullTime && !acceptsPartTime)
        {
            return JobPreferencesErrors.NoEmploymentExtentAccepted;
        }

        return new JobPreferences(acceptsFullTime, acceptsPartTime, excludeStaffingAgencies, commuteLimit);
    }
}
