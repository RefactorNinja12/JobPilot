using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Profiles;

/// <summary>
/// Dealbreakers: hard requirements a job ad must meet to be shown at all.
/// Applied in code before any AI step, and the AI can never override them.
/// </summary>
public sealed record JobPreferences
{
    public const int MaxMonthlySalary = 1_000_000;

    private JobPreferences(
        int? minimumMonthlySalary,
        bool acceptsFullTime,
        bool acceptsPartTime,
        bool excludeStaffingAgencies)
    {
        MinimumMonthlySalary = minimumMonthlySalary;
        AcceptsFullTime = acceptsFullTime;
        AcceptsPartTime = acceptsPartTime;
        ExcludeStaffingAgencies = excludeStaffingAgencies;
    }

    /// <summary>No requirements: every ad passes. Default for new profiles.</summary>
    public static JobPreferences None { get; } = new(null, acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false);

    /// <summary>Minimum salary in SEK per month, or null for no requirement.</summary>
    public int? MinimumMonthlySalary { get; }

    public bool AcceptsFullTime { get; }

    public bool AcceptsPartTime { get; }

    public bool ExcludeStaffingAgencies { get; }

    public static Result<JobPreferences> Create(
        int? minimumMonthlySalary,
        bool acceptsFullTime,
        bool acceptsPartTime,
        bool excludeStaffingAgencies)
    {
        if (minimumMonthlySalary is < 1 or > MaxMonthlySalary)
        {
            return JobPreferencesErrors.SalaryOutOfRange;
        }

        if (!acceptsFullTime && !acceptsPartTime)
        {
            return JobPreferencesErrors.NoEmploymentExtentAccepted;
        }

        return new JobPreferences(minimumMonthlySalary, acceptsFullTime, acceptsPartTime, excludeStaffingAgencies);
    }
}
