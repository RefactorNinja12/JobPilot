using Jobpilot.Domain.Common;

namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// Downloads the Enrichments competence vocabulary (GET /synonymdictionary). It is large (about 7 MB, 58k concepts)
/// and changes rarely, so callers should store the result rather than fetch it per request.
/// Implemented in Infrastructure; faked in Application tests.
/// </summary>
public interface ISkillVocabularyClient
{
    /// <summary>A failure means the API itself could not be used.</summary>
    Task<Result<SkillVocabulary>> GetCompetenciesAsync(CancellationToken cancellationToken);
}
