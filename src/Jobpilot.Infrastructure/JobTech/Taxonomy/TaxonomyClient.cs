using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Polly;

namespace Jobpilot.Infrastructure.JobTech.Taxonomy;

/// <summary>
/// Searches occupations with GET /v1/taxonomy/suggesters/autocomplete (type occupation-name), which also matches
/// alternative labels. Expects <see cref="HttpClient.BaseAddress"/> to be the Taxonomy root URL.
/// </summary>
public sealed class TaxonomyClient(HttpClient httpClient) : IOccupationSearch
{
    public async Task<Result<IReadOnlyList<Occupation>>> SearchAsync(
        string? text,
        int maxResults,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxResults, 1);

        var query = text?.Trim();
        if (string.IsNullOrEmpty(query))
        {
            return Result.Success<IReadOnlyList<Occupation>>([]);
        }

        var uri = new Uri(
            string.Create(
                CultureInfo.InvariantCulture,
                $"v1/taxonomy/suggesters/autocomplete?type=occupation-name&limit={maxResults}&query-string={Uri.EscapeDataString(query)}"),
            UriKind.Relative);

        var concepts = await GetAsync(uri, cancellationToken);
        if (concepts.IsFailure)
        {
            return concepts.Error;
        }

        // Skip incomplete concepts instead of failing the search; the rest are still useful.
        return Result.Success<IReadOnlyList<Occupation>>(
        [
            .. concepts.Value
                .Select(c => Occupation.Create(c.Id, c.PreferredLabel))
                .Where(o => o.IsSuccess)
                .Select(o => o.Value)
                .DistinctBy(o => o.TaxonomyId, StringComparer.Ordinal),
        ]);
    }

    private async Task<Result<IReadOnlyList<TaxonomyConceptDto>>> GetAsync(Uri uri, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return TaxonomyErrors.Unavailable(
                    string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)response.StatusCode}"));
            }

            var body = await response.Content.ReadFromJsonAsync<IReadOnlyList<TaxonomyConceptDto>>(cancellationToken);
            return body is null ? TaxonomyErrors.InvalidResponse : Result.Success(body);
        }
        catch (HttpRequestException ex)
        {
            return TaxonomyErrors.Unavailable(ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation the caller did not ask for.
            return TaxonomyErrors.Unavailable("timeout");
        }
        catch (ExecutionRejectedException ex)
        {
            // Resilience pipeline gave up: total timeout reached or circuit open.
            return TaxonomyErrors.Unavailable(ex.Message);
        }
        catch (JsonException)
        {
            return TaxonomyErrors.InvalidResponse;
        }
    }
}
