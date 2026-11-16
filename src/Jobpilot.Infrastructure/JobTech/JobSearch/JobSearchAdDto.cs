namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary>
/// The subset of a JobSearch ad (GET /search hits, GET /ad/{id}) that Jobpilot uses.
/// Mirrors the JSON shape (snake_case), so every field is nullable: never trust external data.
/// See docs/jobtech-api.md for field coverage.
/// </summary>
public sealed record JobSearchAdDto
{
    public string? Id { get; init; }

    public string? Headline { get; init; }

    public string? WebpageUrl { get; init; }

    /// <summary>Swedish local time without offset, e.g. "2026-11-13T13:15:54".</summary>
    public DateTime? PublicationDate { get; init; }

    /// <summary>Swedish local time without offset, e.g. "2026-11-27T23:59:59".</summary>
    public DateTime? ApplicationDeadline { get; init; }

    public JobSearchDescriptionDto? Description { get; init; }

    public JobSearchEmployerDto? Employer { get; init; }

    public JobSearchAddressDto? WorkplaceAddress { get; init; }

    public JobSearchConceptDto? Occupation { get; init; }

    public JobSearchConceptDto? WorkingHoursType { get; init; }
}
