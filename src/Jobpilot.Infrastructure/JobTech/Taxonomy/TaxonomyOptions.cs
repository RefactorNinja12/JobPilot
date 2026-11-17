namespace Jobpilot.Infrastructure.JobTech.Taxonomy;

/// <summary>
/// Settings bound from the "JobTech:Taxonomy" configuration section.
/// BaseUrl is e.g. https://taxonomy.api.jobtechdev.se/.
/// </summary>
public sealed class TaxonomyOptions : JobTechApiOptions
{
    public const string SectionName = "JobTech:Taxonomy";
}
