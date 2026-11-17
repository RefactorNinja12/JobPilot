using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Polly;

namespace Jobpilot.Infrastructure.JobTech.JobSearch;

/// <summary>
/// Calls GET /search page by page, newest first, until <see cref="JobSearchQuery.MaxResults"/> ads
/// or the last page, and GET /ad/{id} for a single ad. Expects <see cref="HttpClient.BaseAddress"/>
/// to be the JobSearch root URL.
/// </summary>
public sealed class JobSearchClient(HttpClient httpClient) : IJobSearchClient
{
    /// <summary>JobSearch returns at most 100 ads per request.</summary>
    public const int PageSize = 100;

    public async Task<Result<JobSearchResult>> SearchAsync(JobSearchQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var ads = new List<JobAd>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var skipped = 0;

        for (var offset = 0; offset < query.MaxResults; offset += PageSize)
        {
            var limit = Math.Min(PageSize, query.MaxResults - offset);
            var page = await FetchPageAsync(BuildSearchUri(query, offset, limit), cancellationToken);
            if (page.IsFailure)
            {
                return page.Error;
            }

            foreach (var ad in page.Value.Select(JobSearchAdMapper.ToJobAd))
            {
                // A new ad published between two requests shifts the pages; skip the repeat it causes.
                if (ad.IsFailure)
                {
                    skipped++;
                }
                else if (seenIds.Add(ad.Value.Id))
                {
                    ads.Add(ad.Value);
                }
            }

            if (page.Value.Count < limit)
            {
                break;
            }
        }

        return new JobSearchResult(ads, skipped);
    }

    private static Uri BuildSearchUri(JobSearchQuery query, int offset, int limit)
    {
        var uri = new StringBuilder("search?sort=pubdate-desc");
        uri.Append(CultureInfo.InvariantCulture, $"&offset={offset}&limit={limit}");

        foreach (var id in query.OccupationIds)
        {
            uri.Append("&occupation-name=").Append(Uri.EscapeDataString(id));
        }

        if (query.PublishedAfter is { } after)
        {
            var local = TimeZoneInfo.ConvertTime(after, SwedishTime.Zone);
            uri.Append("&published-after=").Append(local.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture));
        }

        return new Uri(uri.ToString(), UriKind.Relative);
    }

    public async Task<Result<JobAd>> GetAdAsync(string adId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(adId);

        var id = adId.Trim();
        var ad = await GetJsonAsync<JobSearchAdDto>(
            new Uri("ad/" + Uri.EscapeDataString(id), UriKind.Relative),
            notFound: JobSearchErrors.AdNotFound(id),
            cancellationToken);
        return ad.IsFailure ? ad.Error : JobSearchAdMapper.ToJobAd(ad.Value);
    }

    private async Task<Result<IReadOnlyList<JobSearchAdDto>>> FetchPageAsync(Uri uri, CancellationToken cancellationToken)
    {
        var page = await GetJsonAsync<JobSearchResponseDto>(uri, notFound: null, cancellationToken);
        if (page.IsFailure)
        {
            return page.Error;
        }

        return page.Value.Hits is { } hits ? Result.Success(hits) : JobSearchErrors.InvalidResponse;
    }

    /// <summary>GETs and reads JSON, turning transport problems into errors. 404 gives <paramref name="notFound"/> when set.</summary>
    private async Task<Result<T>> GetJsonAsync<T>(Uri uri, Error? notFound, CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound && notFound is not null)
            {
                return notFound;
            }

            if (!response.IsSuccessStatusCode)
            {
                return JobSearchErrors.Unavailable(
                    string.Create(CultureInfo.InvariantCulture, $"HTTP {(int)response.StatusCode}"));
            }

            var body = await response.Content.ReadFromJsonAsync<T>(JobTechJson.Options, cancellationToken);
            return body is null ? JobSearchErrors.InvalidResponse : Result.Success(body);
        }
        catch (HttpRequestException ex)
        {
            return JobSearchErrors.Unavailable(ex.Message);
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient.Timeout surfaces as a cancellation the caller did not ask for.
            return JobSearchErrors.Unavailable("timeout");
        }
        catch (ExecutionRejectedException ex)
        {
            // Resilience pipeline gave up: total timeout reached or circuit open.
            return JobSearchErrors.Unavailable(ex.Message);
        }
        catch (JsonException)
        {
            return JobSearchErrors.InvalidResponse;
        }
    }
}
