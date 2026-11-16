namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>
/// One document in the POST /enrichtextdocuments response (a JSON array). Only competencies are read;
/// occupations, traits and geos are also returned but not used yet.
/// </summary>
public sealed record EnrichedDocumentDto(string? DocId, EnrichedCandidatesDto? EnrichedCandidates);
