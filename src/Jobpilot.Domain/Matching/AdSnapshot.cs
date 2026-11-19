using Jobpilot.Domain.Jobs;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// The few facts about an ad worth keeping next to Claude's assessment of it: enough to list the assessment later
/// without asking JobSearch again, and after the ad has been taken down.
/// </summary>
public sealed record AdSnapshot(
    string Headline,
    string Employer,
    string? Municipality,
    Uri Url,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline)
{
    public static AdSnapshot From(JobAd ad)
    {
        ArgumentNullException.ThrowIfNull(ad);
        return new(ad.Headline, ad.Employer.Name, ad.Workplace.Municipality, ad.Url, ad.PublishedAt, ad.ApplicationDeadline);
    }
}
