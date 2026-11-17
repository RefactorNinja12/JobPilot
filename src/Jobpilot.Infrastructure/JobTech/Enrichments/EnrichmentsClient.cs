using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Polly;

namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>
/// Sends all ads in one POST /enrichtextdocuments request and keeps the competencies above the threshold.
/// Expects <see cref="HttpClient.BaseAddress"/> to be the Enrichments root URL.
/// </summary>
public sealed class EnrichmentsClient(HttpClient httpClient) : IEnrichmentsClient
{
    private static readonly Uri _enrichPath = new("enrichtextdocuments", UriKind.Relative);

    public async Task<Result<IReadOnlyList<EnrichedAd>>> ExtractCompetenciesAsync(
        EnrichmentQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = new EnrichmentsRequestDto(
            [.. query.Ads.Select(ad => new EnrichmentsDocumentDto(ad.Id, ad.Headline, ad.Description))]);

        var documents = await PostAsync(request, cancellationToken);
        if (documents.IsFailure)
        {
            return documents.Error;
        }

        var byId = documents.Value
            .Where(doc => doc.DocId is not null)
            .DistinctBy(doc => doc.DocId, StringComparer.Ordinal)
            .ToDictionary(doc => doc.DocId!, StringComparer.Ordinal);

        var enriched = new List<EnrichedAd>(query.Ads.Count);
        foreach (var adId in query.Ads.Select(ad => ad.Id))
        {
            if (!byId.TryGetValue(adId, out var doc))
            {
                return EnrichmentsErrors.MissingDocument(adId);
            }

            enriched.Add(new EnrichedAd(adId, Competencies(doc, query.MinPrediction)));
        }

        return enriched;
    }

    private static List<ExtractedCompetency> Competencies(EnrichedDocumentDto doc, double minPrediction) =>
        [.. (doc.EnrichedCandidates?.Competencies ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.ConceptLabel) && c.Prediction >= minPrediction)
            .OrderByDescending(c => c.Prediction)
            .DistinctBy(c => c.ConceptLabel!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(c => new ExtractedCompetency(c.ConceptLabel!.Trim(), c.Prediction))];

    private async Task<Result<IReadOnlyList<EnrichedDocumentDto>>> PostAsync(
        EnrichmentsRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Enrichments rejects chunked request bodies with HTTP 400, and PostAsJsonAsync streams (chunked).
            // Buffering first sends a Content-Length instead.
            using var content = JsonContent.Create(request, options: JobTechJson.Options);
            await content.LoadIntoBufferAsync(cancellationToken);
            using var response = await httpClient.PostAsync(_enrichPath, content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return EnrichmentsErrors.Unavailable(
                    string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)response.StatusCode}"));
            }

            var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<EnrichedDocumentDto>>(
                JobTechJson.Options,
                cancellationToken);
            return body is null ? EnrichmentsErrors.InvalidResponse : Result.Success(body);
        }
        catch (HttpRequestException ex)
        {
            return EnrichmentsErrors.Unavailable(ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation the caller did not ask for.
            return EnrichmentsErrors.Unavailable("timeout");
        }
        catch (ExecutionRejectedException ex)
        {
            // Resilience pipeline gave up: total timeout reached or circuit open.
            return EnrichmentsErrors.Unavailable(ex.Message);
        }
        catch (JsonException)
        {
            return EnrichmentsErrors.InvalidResponse;
        }
    }
}
