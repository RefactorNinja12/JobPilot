using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Domain.Matching;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>A dictionary standing in for the database-backed assessment store.</summary>
internal sealed class InMemoryAssessmentStore : IAssessmentStore
{
    private readonly Dictionary<string, AiAssessment> _stored = new(StringComparer.Ordinal);

    public Task<IReadOnlyDictionary<string, AiAssessment>> GetAsync(
        IReadOnlyCollection<string> adIds,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<string, AiAssessment>>(
            _stored.Where(s => adIds.Contains(s.Key, StringComparer.Ordinal)).ToDictionary(s => s.Key, s => s.Value, StringComparer.Ordinal));

    public Task SaveAsync(AiAssessment assessment, CancellationToken cancellationToken)
    {
        _stored[assessment.AdId] = assessment;
        return Task.CompletedTask;
    }
}
