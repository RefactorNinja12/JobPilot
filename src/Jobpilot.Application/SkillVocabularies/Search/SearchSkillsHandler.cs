using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;

namespace Jobpilot.Application.SkillVocabularies.Search;

/// <summary>
/// Finds skill concepts whose name or a synonym contains the text, best matches first.
/// Lets the user (or Claude) pick the exact concept name before adding a skill.
/// </summary>
public sealed class SearchSkillsHandler(ISkillVocabularyStore store)
{
    public async Task<Result<IReadOnlyList<SkillConcept>>> HandleAsync(
        SearchSkillsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.MaxResults is < 1 or > SkillVocabulary.MaxSearchResults)
        {
            return SearchSkillsErrors.MaxResultsOutOfRange;
        }

        var vocabulary = await store.GetAsync(cancellationToken);
        if (vocabulary is null)
        {
            return SearchSkillsErrors.VocabularyNotLoaded;
        }

        return Result.Success(vocabulary.Search(query.Text, query.MaxResults));
    }
}
