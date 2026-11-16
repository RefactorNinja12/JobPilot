using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Jobs;

/// <summary>A JobTech Taxonomy occupation, e.g. "Systemutvecklare/Programmerare". Always present in JobSearch ads.</summary>
public sealed record Occupation
{
    private Occupation(string taxonomyId, string label)
    {
        TaxonomyId = taxonomyId;
        Label = label;
    }

    public string TaxonomyId { get; }

    public string Label { get; }

    public static Result<Occupation> Create(string? taxonomyId, string? label)
    {
        var trimmedId = taxonomyId?.Trim();
        if (string.IsNullOrEmpty(trimmedId))
        {
            return JobAdErrors.OccupationIdEmpty;
        }

        var trimmedLabel = label?.Trim();
        if (string.IsNullOrEmpty(trimmedLabel))
        {
            return JobAdErrors.OccupationLabelEmpty;
        }

        return new Occupation(trimmedId, trimmedLabel);
    }
}
