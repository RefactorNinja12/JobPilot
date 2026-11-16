namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary>Settings bound from the "JobTech:JobSearch" configuration section.</summary>
public sealed class JobSearchOptions
{
    public const string SectionName = "JobTech:JobSearch";

    /// <summary>Root URL of the JobSearch API, e.g. https://jobsearch.api.jobtechdev.se/. Required.</summary>
    public Uri? BaseUrl { get; set; }
}
