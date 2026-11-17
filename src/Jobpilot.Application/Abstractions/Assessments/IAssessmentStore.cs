using Jobpilot.Domain.Matching;

namespace Jobpilot.Application.Abstractions.Assessments;

/// <summary>Keeps Claude's assessments per ad. Implemented in Infrastructure; faked in Application tests.</summary>
public interface IAssessmentStore
{
    /// <summary>The stored assessments for those of <paramref name="adIds"/> that have one.</summary>
    Task<IReadOnlyDictionary<string, AiAssessment>> GetAsync(IReadOnlyCollection<string> adIds, CancellationToken cancellationToken);

    /// <summary>Stores the assessment, replacing an earlier one for the same ad.</summary>
    Task SaveAsync(AiAssessment assessment, CancellationToken cancellationToken);
}
