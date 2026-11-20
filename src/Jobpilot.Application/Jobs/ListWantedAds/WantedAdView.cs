namespace Jobpilot.Application.Jobs.ListWantedAds;

/// <summary>One ad the user wants to apply for, with what Claude needs to prepare the application.</summary>
/// <param name="AdId">JobSearch ad id; use it with get_job_ad and save_application_support.</param>
/// <param name="Headline">The ad's title.</param>
/// <param name="Employer">Employer name.</param>
/// <param name="Municipality">Where the job is, or null.</param>
/// <param name="Url">The ad on Platsbanken.</param>
/// <param name="ApplicationDeadline">Last day to apply.</param>
/// <param name="Chance">0-100, Claude's earlier assessment of the chance.</param>
/// <param name="Summary">Claude's earlier reasons.</param>
/// <param name="MatchedSkills">What the ad asks for that the profile had when assessed.</param>
/// <param name="MissingSkills">What the ad asks for that the profile lacked when assessed.</param>
/// <param name="HasSupport">True when application support has already been saved for the ad.</param>
public sealed record WantedAdView(
    string AdId,
    string Headline,
    string Employer,
    string? Municipality,
    Uri Url,
    DateOnly ApplicationDeadline,
    int Chance,
    string Summary,
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    bool HasSupport);
