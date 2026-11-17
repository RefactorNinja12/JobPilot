namespace Jobpilot.Infrastructure.Persistence.Assessments;

/// <summary>One row of ad_assessments.</summary>
internal sealed class StoredAssessment
{
    public required string AdId { get; init; }

    public int Chance { get; set; }

    public string Summary { get; set; } = string.Empty;

    /// <summary>In UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset AssessedAt { get; set; }
}
