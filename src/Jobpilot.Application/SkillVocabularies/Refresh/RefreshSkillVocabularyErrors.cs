using Jobpilot.Domain.Common;

namespace Jobpilot.Application.SkillVocabularies.Refresh;

public static class RefreshSkillVocabularyErrors
{
    public static readonly Error Empty = Error.Unavailable(
        "SkillVocabulary.Empty",
        "Enrichments returned an empty skill vocabulary; the stored one was kept.");
}
