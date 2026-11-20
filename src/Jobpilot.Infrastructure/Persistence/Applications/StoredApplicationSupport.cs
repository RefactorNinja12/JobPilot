namespace Jobpilot.Infrastructure.Persistence.Applications;

/// <summary>One row of application_support: what Claude prepared for one ad.</summary>
internal sealed class StoredApplicationSupport
{
    public required string AdId { get; init; }

    /// <summary>text[]; most important first.</summary>
    public string[] Keywords { get; set; } = [];

    public string Highlights { get; set; } = string.Empty;

    public string? CvSuggestions { get; set; }

    public string? LetterDraft { get; set; }

    public string? CompanyNotes { get; set; }

    /// <summary>In UTC (timestamptz only accepts UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
