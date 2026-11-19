using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Testing;

/// <summary>A dictionary standing in for the database-backed assessment store.</summary>
public sealed class InMemoryAssessmentStore : IAssessmentStore
{
    private readonly Dictionary<string, AssessedAd> _stored = new(StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<string, AiAssessment>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, AiAssessment>>(
            _stored.Where(s => adIds.Contains(s.Key, StringComparer.Ordinal)).ToDictionary(s => s.Key, s => s.Value.Assessment, StringComparer.Ordinal));

    public Task<IReadOnlyList<AssessedAd>> ListAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AssessedAd>>([.. _stored.Values]);

    public Task SaveAsync(AiAssessment assessment, AdSnapshot ad, CancellationToken cancellationToken)
    {
        _stored[assessment.AdId] = new AssessedAd(assessment, ad);
        return Task.CompletedTask;
    }
}
