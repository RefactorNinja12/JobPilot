namespace Jobpilot.Infrastructure.Persistence.Assessments;

/// <summary>One row of ad_assessments.</summary>
internal sealed class StoredAssessment
{
    public required string AdId { get; init; }

    public int Chance { get; set; }

    public string Summary { get; set; } = string.Empty;

    /// <summary>In UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset AssessedAt { get; set; }

    // Snapshot of the ad. Null on assessments saved before snapshots existed.
    public string? Headline { get; set; }

    public string? Employer { get; set; }

    public string? Municipality { get; set; }

    public string? Url { get; set; }

    /// <summary>In UTC.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    public DateOnly? ApplicationDeadline { get; set; }
}
