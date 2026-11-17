namespace Jobpilot.Infrastructure.Employers;

/// <summary>Settings bound from the "StaffingAgencies" configuration section.</summary>
public sealed class StaffingAgencyOptions
{
    public const string SectionName = "StaffingAgencies";

    /// <summary>
    /// Organization numbers of staffing agencies, as "556677-8899" or "5566778899". Empty by default:
    /// the list is maintained by the user, so nothing is excluded until it is filled in.
    /// </summary>
    public IList<string> OrganizationNumbers { get; } = [];
}
