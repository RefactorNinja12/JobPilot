namespace Jobpilot.Domain.Jobs;

/// <summary>Already-validated parts of a job ad, grouped to keep <see cref="JobAd.Create"/> readable.</summary>
public sealed record JobAdDetails(
    Uri Url,
    Employer Employer,
    Workplace Workplace,
    Occupation Occupation,
    WorkingHours WorkingHours,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline);
