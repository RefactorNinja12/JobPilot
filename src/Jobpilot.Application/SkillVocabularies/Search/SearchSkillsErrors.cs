using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.SkillVocabularies.Search;

public static class SearchSkillsErrors
{
    public static readonly Error MaxResultsOutOfRange = Error.Validation(
        "SearchSkills.MaxResultsOutOfRange",
        $"MaxResults must be between 1 and {SkillVocabulary.MaxSearchResults}.");

    public static readonly Error VocabularyNotLoaded = Error.Unavailable(
        "SearchSkills.VocabularyNotLoaded",
        "The skill vocabulary has not been downloaded yet. The Worker downloads it at start.");
}
