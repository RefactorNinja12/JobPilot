namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>Body for POST /enrichtextdocuments. Synonyms and sentences are not used, so they are turned off.</summary>
public sealed record EnrichmentsRequestDto(IReadOnlyList<EnrichmentsDocumentDto> DocumentsInput)
{
    public bool IncludeTermsInfo { get; init; } = true;

    public bool IncludeSentences { get; init; }

    public bool IncludeSynonyms { get; init; }

    public bool IncludeMisspelledSynonyms { get; init; }

    public string SortByPredictionScore { get; init; } = "DESC";
}
