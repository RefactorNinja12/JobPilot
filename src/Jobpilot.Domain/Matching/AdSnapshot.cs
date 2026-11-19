using Jobpilot.Domain.Jobs;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// The few facts about an ad worth keeping next to Claude's assessment of it: enough to list the assessment later
/// without asking JobSearch again, and after the ad has been taken down.
/// </summary>
/// <param name="Headline">The ad's title.</param>
/// <param name="Employer">Employer name.</param>
/// <param name="Municipality">Where the job is, or null if the ad does not say.</param>
/// <param name="Url">The ad on Platsbanken.</param>
/// <param name="PublishedAt">When the ad was published.</param>
/// <param name="ApplicationDeadline">Last day to apply.</param>
/// <param name="MatchedSkills">What the ad asks for that the profile has, as worked out when it was assessed; null if not known.</param>
/// <param name="MissingSkills">What the ad asks for that the profile lacks, as worked out when it was assessed; null if not known.</param>
public sealed record AdSnapshot(
    string Headline,
    string Employer,
    string? Municipality,
    Uri Url,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline,
    IReadOnlyList<string>? MatchedSkills = null,
    IReadOnlyList<string>? MissingSkills = null)
{
    public static AdSnapshot From(JobAd ad)
    {
        ArgumentNullException.ThrowIfNull(ad);
        return new(ad.Headline, ad.Employer.Name, ad.Workplace.Municipality, ad.Url, ad.PublishedAt, ad.ApplicationDeadline);
    }
}
