namespace Jobpilot.Application.Jobs.SaveApplicationSupport;

/// <param name="AdId">The adId of an ad the user has marked as one to apply for.</param>
/// <param name="Keywords">Words and terms from the ad worth using, most important first (1-30, each at most 60 characters).</param>
/// <param name="Highlights">What to lift from the profile and the base CV for this ad, and honest gaps (at most 4,000 characters).</param>
/// <param name="CvSuggestions">Suggested changes to the base CV, or null.</param>
/// <param name="LetterDraft">A short cover letter draft using only facts from the profile and the base documents, or null.</param>
/// <param name="CompanyNotes">What the company does, or null.</param>
public sealed record SaveApplicationSupportCommand(
    string? AdId,
    IReadOnlyList<string>? Keywords,
    string? Highlights,
    string? CvSuggestions,
    string? LetterDraft,
    string? CompanyNotes);
