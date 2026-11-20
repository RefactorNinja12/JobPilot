using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.Applications;

public static class ApplicationSupportErrors
{
    public static readonly Error AdIdEmpty = Error.Validation("ApplicationSupport.AdIdEmpty", "An ad id is required.");

    public static readonly Error KeywordsEmpty =
        Error.Validation("ApplicationSupport.KeywordsEmpty", "At least one keyword is required.");

    public static readonly Error TooManyKeywords = Error.Validation(
        "ApplicationSupport.TooManyKeywords",
        $"Give at most {ApplicationSupport.MaxKeywords} keywords.");

    public static readonly Error KeywordTooLong = Error.Validation(
        "ApplicationSupport.KeywordTooLong",
        $"A keyword must be at most {ApplicationSupport.MaxKeywordLength} characters.");

    public static readonly Error HighlightsEmpty =
        Error.Validation("ApplicationSupport.HighlightsEmpty", "What to lift is required.");

    public static readonly Error TextTooLong = Error.Validation(
        "ApplicationSupport.TextTooLong",
        $"Each text must be at most {ApplicationSupport.MaxTextLength} characters.");
}
