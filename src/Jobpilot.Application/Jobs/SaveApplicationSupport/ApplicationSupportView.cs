using Jobpilot.Domain.Applications;

namespace Jobpilot.Application.Jobs.SaveApplicationSupport;

/// <summary>An ad's application support as stored.</summary>
/// <param name="AdId">The ad.</param>
/// <param name="Keywords">Words and terms from the ad worth using, most important first.</param>
/// <param name="Highlights">What to lift from the profile and the base CV, and honest gaps.</param>
/// <param name="CvSuggestions">Suggested changes to the base CV, or null.</param>
/// <param name="LetterDraft">A short cover letter draft, or null.</param>
/// <param name="CompanyNotes">What the company does, or null.</param>
/// <param name="UpdatedAt">When it was made.</param>
public sealed record ApplicationSupportView(
    string AdId,
    IReadOnlyList<string> Keywords,
    string Highlights,
    string? CvSuggestions,
    string? LetterDraft,
    string? CompanyNotes,
    DateTimeOffset UpdatedAt)
{
    public static ApplicationSupportView From(ApplicationSupport support)
    {
        ArgumentNullException.ThrowIfNull(support);
        return new(support.AdId, support.Keywords, support.Highlights, support.CvSuggestions, support.LetterDraft, support.CompanyNotes, support.UpdatedAt);
    }
}
