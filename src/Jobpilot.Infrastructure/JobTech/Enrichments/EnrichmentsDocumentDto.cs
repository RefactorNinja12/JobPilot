namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>One ad sent to Enrichments. <see cref="DocId"/> is the JobSearch ad id, echoed back in the response.</summary>
public sealed record EnrichmentsDocumentDto(string DocId, string DocHeadline, string DocText);
