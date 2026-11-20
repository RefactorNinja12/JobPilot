namespace Jobpilot.Application.Jobs.ListAssessedJobs;

/// <summary>One ad Claude has assessed, as stored: no call to JobSearch is needed to show it.</summary>
/// <param name="AdId">JobSearch ad id.</param>
/// <param name="Headline">The ad's title.</param>
/// <param name="Employer">Employer name.</param>
/// <param name="Municipality">Where the job is, or null if the ad does not say.</param>
/// <param name="Url">The ad on Platsbanken.</param>
/// <param name="PublishedAt">When the ad was published.</param>
/// <param name="ApplicationDeadline">Last day to apply.</param>
/// <param name="Chance">0-100, Claude's judgement of the chance of getting the job.</param>
/// <param name="Summary">Claude's reasons, in a few sentences.</param>
/// <param name="AssessedAt">When Claude assessed the ad.</param>
/// <param name="MatchedSkills">What the ad asks for that the profile had when assessed; empty when not known.</param>
/// <param name="MissingSkills">What the ad asks for that the profile lacked when assessed; empty when not known.</param>
/// <param name="Interested">True when the user has marked the ad as one they want to apply for.</param>
public sealed record AssessedJobView(
    string AdId,
    string Headline,
    string Employer,
    string? Municipality,
    Uri Url,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline,
    int Chance,
    string Summary,
    DateTimeOffset AssessedAt,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    bool Interested);
