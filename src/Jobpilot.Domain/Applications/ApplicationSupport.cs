using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Applications;

/// <summary>
/// What Claude has prepared for the user's application to one ad: the words to use, what to lift from the profile and the
/// base CV, suggested CV changes, a short letter draft and notes on what the company does. Produced outside the
/// deterministic code, so it is validated like any other input. One per ad; saving again replaces it.
/// </summary>
public sealed record ApplicationSupport
{
    public const int MaxKeywords = 30;
    public const int MaxKeywordLength = 60;
    public const int MaxTextLength = 4000;

    private ApplicationSupport(
        string adId,
        IReadOnlyList<string> keywords,
        string highlights,
        string? cvSuggestions,
        string? letterDraft,
        string? companyNotes,
        DateTimeOffset updatedAt)
    {
        AdId = adId;
        Keywords = keywords;
        Highlights = highlights;
        CvSuggestions = cvSuggestions;
        LetterDraft = letterDraft;
        CompanyNotes = companyNotes;
        UpdatedAt = updatedAt;
    }

    public string AdId { get; }

    /// <summary>Words and terms from the ad worth using in the CV and letter, most important first. At least one.</summary>
    public IReadOnlyList<string> Keywords { get; }

    /// <summary>Which of the user's experience and skills to lift for this ad, and honest gaps. Required.</summary>
    public string Highlights { get; }

    /// <summary>Concrete changes to the base CV for this ad, if any.</summary>
    public string? CvSuggestions { get; }

    /// <summary>A short cover letter draft built only on facts from the profile and the base documents, if any.</summary>
    public string? LetterDraft { get; }

    /// <summary>What the company does, from the ad and public sources, if any.</summary>
    public string? CompanyNotes { get; }

    public DateTimeOffset UpdatedAt { get; }

    public static Result<ApplicationSupport> Create(
        string? adId,
        IEnumerable<string>? keywords,
        string? highlights,
        string? cvSuggestions,
        string? letterDraft,
        string? companyNotes,
        DateTimeOffset updatedAt)
    {
        var id = adId?.Trim();
        if (string.IsNullOrEmpty(id))
        {
            return ApplicationSupportErrors.AdIdEmpty;
        }

        var cleaned = (keywords ?? [])
            .Select(k => k?.Trim() ?? string.Empty)
            .Where(k => k.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (cleaned.Count == 0)
        {
            return ApplicationSupportErrors.KeywordsEmpty;
        }

        if (cleaned.Count > MaxKeywords)
        {
            return ApplicationSupportErrors.TooManyKeywords;
        }

        if (cleaned.Any(k => k.Length > MaxKeywordLength))
        {
            return ApplicationSupportErrors.KeywordTooLong;
        }

        var highlightsText = highlights?.Trim();
        if (string.IsNullOrEmpty(highlightsText))
        {
            return ApplicationSupportErrors.HighlightsEmpty;
        }

        var optional = new[] { highlightsText, NullIfBlank(cvSuggestions), NullIfBlank(letterDraft), NullIfBlank(companyNotes) };
        if (optional.Any(t => t is { Length: > MaxTextLength }))
        {
            return ApplicationSupportErrors.TextTooLong;
        }

        return new ApplicationSupport(id, cleaned, highlightsText, optional[1], optional[2], optional[3], updatedAt);
    }

    private static string? NullIfBlank(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
