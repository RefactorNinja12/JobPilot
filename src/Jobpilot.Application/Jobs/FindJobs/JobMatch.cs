using Jobpilot.Application.Jobs.SaveAssessment;

namespace Jobpilot.Application.Jobs.FindJobs;

/// <summary>
/// Compact summary of one ad that passed the dealbreakers, for Claude to read. No description text: that is fetched
/// separately and only for the ads Claude needs.
/// </summary>
/// <param name="AdId">JobSearch ad id.</param>
/// <param name="Headline">The ad's title.</param>
/// <param name="Employer">Employer name.</param>
/// <param name="Municipality">Where the job is, or null if the ad does not say.</param>
/// <param name="Occupation">The ad's Taxonomy occupation label.</param>
/// <param name="WorkingHours">FullTime, PartTime or Unknown.</param>
/// <param name="PublishedAt">When the ad was published.</param>
/// <param name="ApplicationDeadline">Last day to apply.</param>
/// <param name="Url">The ad on Platsbanken.</param>
/// <param name="FitScore">0-100, how well the profile's skills cover what the ad asks for.</param>
/// <param name="MatchedSkills">Requested skills the profile has, heaviest first.</param>
/// <param name="MissingSkills">Requested skills the profile lacks, heaviest first.</param>
/// <param name="Unknowns">Dealbreakers that could not be checked, e.g. "Location" when the ad has no coordinates.</param>
/// <param name="Assessment">Claude's earlier assessment of this ad, or null if it has not been assessed.</param>
public sealed record JobMatch(
    string AdId,
    string Headline,
    string Employer,
    string? Municipality,
    string Occupation,
    string WorkingHours,
    DateTimeOffset PublishedAt,
    DateOnly ApplicationDeadline,
    Uri Url,
    int FitScore,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Unknowns,
    AssessmentView? Assessment = null);
