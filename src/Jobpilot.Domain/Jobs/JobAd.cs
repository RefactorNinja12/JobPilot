using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Jobs;

/// <summary>
/// A job ad from Platsbanken (JobSearch). Identified by the JobSearch ad id.
/// Skills are not part of the ad itself: they are extracted from the description by Enrichments later.
/// </summary>
public sealed class JobAd
{
    private JobAd(string id, string headline, string description, JobAdDetails details)
    {
        Id = id;
        Headline = headline;
        Description = description;
        Url = details.Url;
        Employer = details.Employer;
        Workplace = details.Workplace;
        Occupation = details.Occupation;
        WorkingHours = details.WorkingHours;
        PublishedAt = details.PublishedAt;
        ApplicationDeadline = details.ApplicationDeadline;
    }

    /// <summary>JobSearch ad id, e.g. "31727571".</summary>
    public string Id { get; }

    public string Headline { get; }

    /// <summary>Plain-text description. May be empty.</summary>
    public string Description { get; }

    public Employer Employer { get; }

    public Workplace Workplace { get; }

    public Occupation Occupation { get; }

    public WorkingHours WorkingHours { get; }

    public DateTimeOffset PublishedAt { get; }

    /// <summary>Last day to apply (inclusive).</summary>
    public DateOnly ApplicationDeadline { get; }

    /// <summary>Public page for the ad (Platsbanken), as provided by JobSearch.</summary>
    public Uri Url { get; }

    public static Result<JobAd> Create(string? id, string? headline, string? description, JobAdDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var trimmedId = id?.Trim();
        if (string.IsNullOrEmpty(trimmedId))
        {
            return JobAdErrors.IdEmpty;
        }

        var trimmedHeadline = headline?.Trim();
        if (string.IsNullOrEmpty(trimmedHeadline))
        {
            return JobAdErrors.HeadlineEmpty;
        }

        if (!details.Url.IsAbsoluteUri)
        {
            return JobAdErrors.UrlNotAbsolute;
        }

        if (!Enum.IsDefined(details.WorkingHours))
        {
            return JobAdErrors.WorkingHoursInvalid;
        }

        if (details.ApplicationDeadline < DateOnly.FromDateTime(details.PublishedAt.Date))
        {
            return JobAdErrors.DeadlineBeforePublication;
        }

        return new JobAd(trimmedId, trimmedHeadline, description?.Trim() ?? string.Empty, details);
    }

    /// <summary>True while it is still possible to apply on the given day.</summary>
    public bool IsOpenOn(DateOnly date) => date <= ApplicationDeadline;
}
