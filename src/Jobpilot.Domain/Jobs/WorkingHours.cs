namespace Jobpilot.Domain.Jobs;

/// <summary>Extent of the job. Unknown when the ad does not say (about 1% of JobSearch ads).</summary>
public enum WorkingHours
{
    Unknown = 0,
    FullTime = 1,
    PartTime = 2,
}
