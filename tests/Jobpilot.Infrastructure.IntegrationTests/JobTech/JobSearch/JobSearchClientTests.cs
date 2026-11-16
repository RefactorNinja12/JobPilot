using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Infrastructure.JobTech.JobSearch;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch.JobSearchResponses;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch;

/// <summary>Runs the real HTTP client against WireMock standing in for JobSearch.</summary>
public sealed class JobSearchClientTests : IDisposable
{
    private const string Developer = "fg7B_yov_smw";

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HttpClient _http;
    private readonly JobSearchClient _client;

    public JobSearchClientTests()
    {
        _http = new HttpClient { BaseAddress = new Uri(_server.Url! + "/") };
        _client = new JobSearchClient(_http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _http.Dispose();
        _server.Dispose();
    }

    private static JobSearchQuery Query(int maxResults = 100, DateTimeOffset? publishedAfter = null) =>
        JobSearchQuery.Create([Developer], maxResults, publishedAfter).Value;

    private void RespondTo(string offset, string body, int status = 200) =>
        _server
            .Given(Request.Create().WithPath("/search").WithParam("offset", offset).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    private Dictionary<string, string> RequestParams(int index) =>
        _server.LogEntries[index].RequestMessage!.Query!
            .ToDictionary(p => p.Key, p => string.Join(',', p.Value), StringComparer.Ordinal);

    [Fact]
    public async Task Search_SendsOccupationsPagingAndNewestFirst()
    {
        RespondTo("0", Body());
        var query = JobSearchQuery.Create([Developer, "test_occ_02"], 50).Value;

        await _client.SearchAsync(query, Ct);

        var sent = RequestParams(0);
        sent["occupation-name"].ShouldBe($"{Developer},test_occ_02");
        sent["offset"].ShouldBe("0");
        sent["limit"].ShouldBe("50");
        sent["sort"].ShouldBe("pubdate-desc");
        sent.ShouldNotContainKey("published-after");
    }

    [Fact]
    public async Task Search_SendsPublishedAfterAsSwedishLocalTime()
    {
        RespondTo("0", Body());

        // 12:00 UTC in November is 13:00 in Sweden (CET).
        await _client.SearchAsync(Query(publishedAfter: new DateTimeOffset(2026, 11, 10, 12, 0, 0, TimeSpan.Zero)), Ct);

        RequestParams(0)["published-after"].ShouldBe("2026-11-10T13:00:00");
    }

    [Fact]
    public async Task Search_MapsRealAds()
    {
        RespondTo("0", Body(Ad("fulltime-with-coordinates"), Ad("without-coordinates")));

        var result = (await _client.SearchAsync(Query(), Ct)).Value;

        result.Ads.Select(a => a.Id).ShouldBe(["31727571", "31532394"]);
        result.Ads[0].WorkingHours.ShouldBe(WorkingHours.FullTime);
        result.SkippedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Search_SkipsAndCountsAdsThatCannotBeMapped()
    {
        var broken = Ad("parttime", "broken");
        broken.Remove("application_deadline");
        RespondTo("0", Body(Ad("fulltime-with-coordinates"), broken));

        var result = (await _client.SearchAsync(Query(), Ct)).Value;

        result.Ads.Count.ShouldBe(1);
        result.SkippedCount.ShouldBe(1);
    }

    [Fact]
    public async Task Search_FetchesNextPageUntilAShortPage()
    {
        RespondTo("0", Page(100, "a"));
        RespondTo("100", Page(30, "b"));

        var result = (await _client.SearchAsync(Query(maxResults: 500), Ct)).Value;

        result.Ads.Count.ShouldBe(130);
        _server.LogEntries.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Search_StopsAtMaxResults()
    {
        RespondTo("0", Page(100, "a"));
        RespondTo("100", Page(50, "b"));

        var result = (await _client.SearchAsync(Query(maxResults: 150), Ct)).Value;

        result.Ads.Count.ShouldBe(150);
        _server.LogEntries.Count.ShouldBe(2);
        RequestParams(1)["limit"].ShouldBe("50");
    }

    [Fact]
    public async Task Search_DropsAdRepeatedOnNextPage()
    {
        // A newly published ad shifts the pages, so the last ad of page 1 comes back first on page 2.
        RespondTo("0", Page(100, "a"));
        RespondTo("100", Body(Ad("parttime", "a-99"), Ad("parttime", "b-0")));

        var result = (await _client.SearchAsync(Query(maxResults: 200), Ct)).Value;

        result.Ads.Count.ShouldBe(101);
        result.Ads.Select(a => a.Id).ShouldBeUnique(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(400)]
    public async Task Search_ErrorStatus_FailsAsUnavailable(int status)
    {
        RespondTo("0", "{}", status);

        var error = (await _client.SearchAsync(Query(), Ct)).Error;

        error.Code.ShouldBe("JobSearch.Unavailable");
        error.Type.ShouldBe(ErrorType.Unavailable);
        error.Message.ShouldContain(status.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"total":{"value":0}}""")]
    public async Task Search_UnreadableBody_FailsWithInvalidResponse(string body)
    {
        RespondTo("0", body);

        (await _client.SearchAsync(Query(), Ct)).Error.ShouldBe(JobSearchErrors.InvalidResponse);
    }

    [Fact]
    public async Task Search_FailureOnLaterPage_FailsWholeSearch()
    {
        RespondTo("0", Page(100, "a"));
        RespondTo("100", "{}", 502);

        (await _client.SearchAsync(Query(maxResults: 200), Ct)).Error.Code.ShouldBe("JobSearch.Unavailable");
    }

    [Fact]
    public async Task Search_Timeout_FailsAsUnavailable()
    {
        _server
            .Given(Request.Create().WithPath("/search").UsingGet())
            .RespondWith(Response.Create().WithBody(Body()).WithDelay(TimeSpan.FromSeconds(2)));
        _http.Timeout = TimeSpan.FromMilliseconds(200);

        (await _client.SearchAsync(Query(), Ct)).Error.Message.ShouldContain("timeout");
    }

    [Fact]
    public async Task Search_ServerDown_FailsAsUnavailable()
    {
        _server.Stop();

        (await _client.SearchAsync(Query(), Ct)).Error.Code.ShouldBe("JobSearch.Unavailable");
    }

    [Fact]
    public async Task Search_CancelledByCaller_Throws()
    {
        RespondTo("0", Body());
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _client.SearchAsync(Query(), cancelled.Token));
    }
}
