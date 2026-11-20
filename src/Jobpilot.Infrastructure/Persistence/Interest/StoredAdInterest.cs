namespace Jobpilot.Infrastructure.Persistence.Interest;

/// <summary>One row of ad_interests: an ad the user wants to apply for.</summary>
internal sealed class StoredAdInterest
{
    public required string AdId { get; init; }

    /// <summary>In UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset MarkedAt { get; set; }
}
