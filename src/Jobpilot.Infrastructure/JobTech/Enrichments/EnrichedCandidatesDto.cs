namespace Jobpilot.Infrastructure.JobTech.Enrichments;

public sealed record EnrichedCandidatesDto(IReadOnlyList<EnrichedCandidateDto>? Competencies);
