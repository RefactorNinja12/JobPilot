using Jobpilot.Domain.Common;
using Jobpilot.Infrastructure.JobTech.JobSearch;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch.JobSearchResponses;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.JobSearch;

/// <summary>GET /ad/{id} against WireMock. The real endpoint returns the same fields as a /search hit.</summary>
public sealed class JobSearchGetAdTests : IDisposable
{
    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HttpClient _http;
    private readonly JobSearchClient _client;

    public JobSearchGetAdTests()
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

    private void RespondTo(string path, string body, int status = 200) =>
        _server.Given(Request.Create().WithPath(path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    [Fact]
    public async Task GetAd_MapsTheAd_IncludingItsDescription()
    {
        RespondTo("/ad/31744367", Ad("parttime", "31744367").ToJsonString());

        var ad = (await _client.GetAdAsync(" 31744367 ", Ct)).Value;

        ad.Id.ShouldBe("31744367");
        ad.Description.ShouldNotBeNullOrWhiteSpace();
        _server.LogEntries.ShouldHaveSingleItem().RequestMessage!.Path.ShouldBe("/ad/31744367");
    }

    [Fact]
    public async Task GetAd_Unknown_FailsAsNotFound()
    {
        RespondTo("/ad/0000000", """{"message": "Ad not found"}""", 404);

        var error = (await _client.GetAdAsync("0000000", Ct)).Error;

        error.ShouldBe(JobSearchErrors.AdNotFound("0000000"));
        error.Type.ShouldBe(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetAd_ServerError_FailsAsUnavailable()
    {
        RespondTo("/ad/1", "{}", 500);

        var error = (await _client.GetAdAsync("1", Ct)).Error;

        error.Code.ShouldBe("JobSearch.Unavailable");
        error.Message.ShouldContain("500");
    }

    [Fact]
    public async Task GetAd_AdThatCannotBeMapped_FailsWithTheMappingError()
    {
        var ad = Ad("parttime", "1");
        ad.Remove("publication_date");
        RespondTo("/ad/1", ad.ToJsonString());

        (await _client.GetAdAsync("1", Ct)).Error.Code.ShouldBe("JobSearch.MissingField");
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    public async Task GetAd_UnreadableBody_FailsWithInvalidResponse(string body)
    {
        RespondTo("/ad/1", body);

        (await _client.GetAdAsync("1", Ct)).Error.ShouldBe(JobSearchErrors.InvalidResponse);
    }
}
