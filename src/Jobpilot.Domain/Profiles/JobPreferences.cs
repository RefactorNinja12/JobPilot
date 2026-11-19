using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Matching;

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
        CommuteLimit? commuteLimit,
        bool excludeSeniorRoles)
    {
        AcceptsFullTime = acceptsFullTime;
        AcceptsPartTime = acceptsPartTime;
        ExcludeStaffingAgencies = excludeStaffingAgencies;
        CommuteLimit = commuteLimit;
        ExcludeSeniorRoles = excludeSeniorRoles;
    }

    /// <summary>No requirements: every ad passes. Default for new profiles.</summary>
    public static JobPreferences None { get; } =
        new(acceptsFullTime: true, acceptsPartTime: true, excludeStaffingAgencies: false, commuteLimit: null, excludeSeniorRoles: false);

    public bool AcceptsFullTime { get; }

    public bool AcceptsPartTime { get; }

    public bool ExcludeStaffingAgencies { get; }

    /// <summary>Hide ads whose title says senior, lead, architect or manager (see <see cref="SeniorTitles"/>).</summary>
    public bool ExcludeSeniorRoles { get; }

    /// <summary>Maximum commute distance from home, or null for no requirement.</summary>
    public CommuteLimit? CommuteLimit { get; }

    public static Result<JobPreferences> Create(
        bool acceptsFullTime,
        bool acceptsPartTime,
        bool excludeStaffingAgencies,
        CommuteLimit? commuteLimit = null,
        bool excludeSeniorRoles = false)
    {
        if (!acceptsFullTime && !acceptsPartTime)
        {
            return JobPreferencesErrors.NoEmploymentExtentAccepted;
        }

        return new JobPreferences(acceptsFullTime, acceptsPartTime, excludeStaffingAgencies, commuteLimit, excludeSeniorRoles);
    }

    /// <summary>
    /// Checks an ad against these dealbreakers. Missing information never rejects: an ad without working hours or
    /// coordinates passes with an <see cref="UnknownDetail"/> flag.
    /// </summary>
    /// <param name="ad">The ad to check.</param>
    /// <param name="isStaffingAgency">Whether the ad's employer is a staffing agency; decided outside the domain.</param>
    public DealbreakerVerdict Check(JobAd ad, bool isStaffingAgency)
    {
        ArgumentNullException.ThrowIfNull(ad);

        var rejections = new List<RejectionReason>();
        var unknowns = new List<UnknownDetail>();

        switch (ad.WorkingHours)
        {
            case WorkingHours.FullTime when !AcceptsFullTime:
                rejections.Add(RejectionReason.FullTimeNotAccepted);
                break;
            case WorkingHours.PartTime when !AcceptsPartTime:
                rejections.Add(RejectionReason.PartTimeNotAccepted);
                break;
            case WorkingHours.Unknown:
                unknowns.Add(UnknownDetail.WorkingHours);
                break;
        }

        if (CommuteLimit is not null)
        {
            if (ad.Workplace.Coordinates is not { } workplace)
            {
                unknowns.Add(UnknownDetail.Location);
            }
            else if (!CommuteLimit.IsWithinReach(workplace))
            {
                rejections.Add(RejectionReason.TooFarAway);
            }
        }

        if (ExcludeStaffingAgencies && isStaffingAgency)
        {
            rejections.Add(RejectionReason.StaffingAgency);
        }

        if (ExcludeSeniorRoles && SeniorTitles.IsSenior(ad.Headline))
        {
            rejections.Add(RejectionReason.TooSenior);
        }

        return new DealbreakerVerdict(rejections, unknowns);
    }
}
