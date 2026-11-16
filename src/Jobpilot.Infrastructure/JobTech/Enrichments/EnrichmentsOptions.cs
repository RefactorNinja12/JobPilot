namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>
/// Settings bound from the "JobTech:Enrichments" configuration section.
/// BaseUrl is e.g. https://jobad-enrichments-api.jobtechdev.se/.
/// </summary>
public sealed class EnrichmentsOptions : JobTechApiOptions
{
    public const string SectionName = "JobTech:Enrichments";
}
