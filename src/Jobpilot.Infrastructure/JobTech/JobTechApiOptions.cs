namespace Jobpilot.Infrastructure.JobTech;

/// <summary>Settings shared by all JobTech API clients.</summary>
public abstract class JobTechApiOptions
{
    /// <summary>Root URL of the API, e.g. https://jobsearch.api.jobtechdev.se/. Required.</summary>
    public Uri? BaseUrl { get; set; }
}
