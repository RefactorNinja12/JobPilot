using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Polly;

namespace Jobpilot.Infrastructure.JobTech.Enrichments;

/// <summary>
/// Downloads the correctly spelled competence terms from GET /synonymdictionary and groups them by concept.
/// Cleaning (trimming, blanks, case-duplicates) is left to <see cref="SkillVocabulary.Create"/>.
/// Expects <see cref="HttpClient.BaseAddress"/> to be the Enrichments root URL.
/// </summary>
public sealed class SkillVocabularyClient(HttpClient httpClient) : ISkillVocabularyClient
{
    private static readonly Uri _dictionaryPath =
        new("synonymdictionary?type=COMPETENCE&spelling=CORRECTLY_SPELLED", UriKind.Relative);

    public async Task<Result<SkillVocabulary>> GetCompetenciesAsync(CancellationToken cancellationToken)
    {
        var dictionary = await GetAsync(cancellationToken);
        if (dictionary.IsFailure)
        {
            return dictionary.Error;
        }

        if (dictionary.Value is not { BuildDatetime: { } builtAt, Items: { } items })
        {
            return EnrichmentsErrors.InvalidResponse;
        }

        var concepts = items
            .Where(item => item.Concept is not null)
            .GroupBy(item => item.Concept!, StringComparer.Ordinal)
            .Select(g => new SkillConcept(g.Key, [.. g.Select(item => item.Term).OfType<string>()]));

        return SkillVocabulary.Create(SwedishTime.FromLocal(builtAt), concepts);
    }

    private async Task<Result<SynonymDictionaryDto>> GetAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(_dictionaryPath, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return EnrichmentsErrors.Unavailable(
                    string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)response.StatusCode}"));
            }

            var body = await response.Content.ReadFromJsonAsync<SynonymDictionaryDto>(JobTechJson.Options, cancellationToken);
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
