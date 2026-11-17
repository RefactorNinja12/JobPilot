using Jobpilot.Domain.Common;
using Jobpilot.Infrastructure.JobTech.Taxonomy;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Taxonomy;

/// <summary>Runs the real Taxonomy client against WireMock standing in for the autocomplete endpoint.</summary>
public sealed class TaxonomyClientTests : IDisposable
{
    private const string Path = "/v1/taxonomy/suggesters/autocomplete";

    /// <summary>Real response for "utvecklare", captured 2026-11-17.</summary>
    private const string RealBody = """
        [
          {"taxonomy/id":"U1YF_JRy_qoo","taxonomy/type":"occupation-name","taxonomy/preferred-label":"Administrativ utvecklare"},
          {"taxonomy/id":"71Ji_irM_rSJ","taxonomy/type":"occupation-name","taxonomy/preferred-label":"Fullstack-utvecklare"},
          {"taxonomy/id":"7wdX_4rv_33z","taxonomy/type":"occupation-name","taxonomy/preferred-label":"Backend-utvecklare"}
        ]
        """;

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HttpClient _http;
    private readonly TaxonomyClient _client;

    public TaxonomyClientTests()
    {
        _http = new HttpClient { BaseAddress = new Uri(_server.Url! + "/") };
        _client = new TaxonomyClient(_http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _http.Dispose();
        _server.Dispose();
    }

    private void RespondWith(string body, int status = 200) =>
        _server.Given(Request.Create().WithPath(Path).UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    [Fact]
    public async Task Search_SendsTextTypeAndLimit()
    {
        RespondWith(RealBody);

        await _client.SearchAsync("  backend utvecklare ", 7, Ct);

        var query = _server.LogEntries.ShouldHaveSingleItem().RequestMessage!.Query!;
        query["query-string"].ShouldBe(["backend utvecklare"]);
        query["type"].ShouldBe(["occupation-name"]);
        query["limit"].ShouldBe(["7"]);
    }

    [Fact]
    public async Task Search_MapsIdsAndLabels()
    {
        RespondWith(RealBody);

        var occupations = (await _client.SearchAsync("utvecklare", 10, Ct)).Value;

        occupations.Select(o => (o.TaxonomyId, o.Label)).ShouldBe(
        [
            ("U1YF_JRy_qoo", "Administrativ utvecklare"),
            ("71Ji_irM_rSJ", "Fullstack-utvecklare"),
            ("7wdX_4rv_33z", "Backend-utvecklare"),
        ]);
    }

    [Fact]
    public async Task Search_SkipsIncompleteAndDuplicateConcepts()
    {
        RespondWith("""
            [
              {"taxonomy/id":"7wdX_4rv_33z","taxonomy/preferred-label":"Backend-utvecklare"},
              {"taxonomy/id":"7wdX_4rv_33z","taxonomy/preferred-label":"Backend-utvecklare"},
              {"taxonomy/preferred-label":"No id"},
              {"taxonomy/id":"no-label"}
            ]
            """);

        (await _client.SearchAsync("backend", 10, Ct)).Value.ShouldHaveSingleItem().TaxonomyId.ShouldBe("7wdX_4rv_33z");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task Search_BlankText_ReturnsNothingWithoutCalling(string? text)
    {
        (await _client.SearchAsync(text, 10, Ct)).Value.ShouldBeEmpty();

        _server.LogEntries.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Search_UnreadableBody_FailsWithInvalidResponse(string body)
    {
        RespondWith(body);

        (await _client.SearchAsync("utvecklare", 10, Ct)).Error.ShouldBe(TaxonomyErrors.InvalidResponse);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(400)]
    public async Task Search_ErrorStatus_FailsAsUnavailable(int status)
    {
        RespondWith("[]", status);

        var error = (await _client.SearchAsync("utvecklare", 10, Ct)).Error;

        error.Code.ShouldBe("Taxonomy.Unavailable");
        error.Type.ShouldBe(ErrorType.Unavailable);
    }

    [Fact]
    public async Task Search_Timeout_FailsAsUnavailable()
    {
        _server.Given(Request.Create().WithPath(Path).UsingGet())
            .RespondWith(Response.Create().WithBody(RealBody).WithDelay(TimeSpan.FromSeconds(2)));
        _http.Timeout = TimeSpan.FromMilliseconds(200);

        (await _client.SearchAsync("utvecklare", 10, Ct)).Error.Message.ShouldContain("timeout");
    }

    [Fact]
    public async Task Search_ServerDown_FailsAsUnavailable()
    {
        _server.Stop();

        (await _client.SearchAsync("utvecklare", 10, Ct)).Error.Code.ShouldBe("Taxonomy.Unavailable");
    }

    [Fact]
    public async Task Search_CancelledByCaller_Throws()
    {
        RespondWith(RealBody);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _client.SearchAsync("utvecklare", 10, cancelled.Token));
    }
}
