using Jobpilot.Domain.Jobs;

namespace Jobpilot.Application.Jobs.GetJobAd;

/// <summary>One ad in full, including the description. ~930 tokens on average, so fetched one at a time.</summary>
/// <param name="AdId">JobSearch ad id.</param>
/// <param name="Headline">The ad's title.</param>
/// <param name="Employer">Employer name.</param>
/// <param name="Municipality">Where the job is, or null if the ad does not say.</param>
/// <param name="Occupation">The ad's Taxonomy occupation label.</param>
/// <param name="WorkingHours">FullTime, PartTime or Unknown.</param>
/// <param name="PublishedAt">When the ad was published.</param>
/// <param name="ApplicationDeadline">Last day to apply.</param>
/// <param name="Url">The ad on Platsbanken.</param>
/// <param name="Description">The ad's full text.</param>
public sealed record JobAdView(
    string AdId,
    string Headline,
    string Employer,
    string? Municipality,
    string Occupation,
    string WorkingHours,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline,
    Uri Url,
    string Description)
{
    public static JobAdView From(JobAd ad)
    {
        ArgumentNullException.ThrowIfNull(ad);
        return new(
            ad.Id,
            ad.Headline,
            ad.Employer.Name,
            ad.Workplace.Municipality,
            ad.Occupation.Label,
            ad.WorkingHours.ToString(),
            ad.PublishedAt,
            ad.ApplicationDeadline,
            ad.Url,
            ad.Description);
    }
}
