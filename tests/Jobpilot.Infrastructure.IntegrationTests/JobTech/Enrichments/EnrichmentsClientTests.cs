using System.Text.Json.Nodes;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Infrastructure.JobTech.Enrichments;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using static Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments.EnrichmentsResponses;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments;

/// <summary>Runs the real HTTP client against WireMock standing in for JobAd Enrichments.</summary>
public sealed class EnrichmentsClientTests : IDisposable
{
    private static readonly JobAdDetails _details = new(
        new Uri("https://arbetsformedlingen.se/platsbanken/annonser/31727571"),
        Employer.Create("Skatteverket", "2021005448").Value,
        Workplace.Unknown,
        Occupation.Create("fg7B_yov_smw", "Systemutvecklare/Programmerare").Value,
        WorkingHours.FullTime,
        new DateTimeOffset(2026, 11, 13, 13, 15, 54, TimeSpan.FromHours(1)),
        new DateOnly(2026, 11, 27));

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HttpClient _http;
    private readonly EnrichmentsClient _client;

    public EnrichmentsClientTests()
    {
        _http = new HttpClient { BaseAddress = new Uri(_server.Url! + "/") };
        _client = new EnrichmentsClient(_http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _http.Dispose();
        _server.Dispose();
    }

    private static JobAd Ad(string id, string headline = "Backendutvecklare", string description = "Vi söker C#.") =>
        JobAd.Create(id, headline, description, _details).Value;

    private static EnrichmentQuery Query(params string[] ids) =>
        EnrichmentQuery.Create(ids.Select(id => Ad(id))).Value;

    private static EnrichmentQuery Query(double minPrediction, string id = "1") =>
        EnrichmentQuery.Create([Ad(id)], minPrediction).Value;

    private void RespondWith(string body, int status = 200) =>
        _server
            .Given(Request.Create().WithPath("/enrichtextdocuments").UsingPost())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    [Fact]
    public async Task Extract_SendsAllAdsInOneRequest()
    {
        RespondWith(Body(Real("1"), Real("2")));
        var query = EnrichmentQuery.Create([Ad("1", "Utvecklare", "C# och .NET"), Ad("2", "Testare", "")]).Value;

        await _client.ExtractCompetenciesAsync(query, Ct);

        _server.LogEntries.Count.ShouldBe(1);
        var sent = JsonNode.Parse(_server.LogEntries[0].RequestMessage!.Body!)!;
        var documents = sent["documents_input"]!.AsArray();
        documents.Count.ShouldBe(2);
        documents[0]!["doc_id"]!.GetValue<string>().ShouldBe("1");
        documents[0]!["doc_headline"]!.GetValue<string>().ShouldBe("Utvecklare");
        documents[0]!["doc_text"]!.GetValue<string>().ShouldBe("C# och .NET");
        documents[1]!["doc_text"]!.GetValue<string>().ShouldBe("");
        sent["include_synonyms"]!.GetValue<bool>().ShouldBeFalse();
        sent["sort_by_prediction_score"]!.GetValue<string>().ShouldBe("DESC");
    }

    [Fact]
    public async Task Extract_MapsCompetenciesFromRealResponse()
    {
        RespondWith(Body(Real("1")));

        var ad = (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Value.ShouldHaveSingleItem();

        ad.AdId.ShouldBe("1");
        ad.Competencies.Select(c => c.Label).ShouldBe(["C#", ".net", "Postgresql"]);
        ad.Competencies[0].Prediction.ShouldBe(0.9694707841);
    }

    [Fact]
    public async Task Extract_DropsCompetenciesBelowThreshold_KeepingTheBoundary()
    {
        RespondWith(Body(Document("1", ("C#", 0.9), ("Docker", 0.7), ("Excel", 0.69))));

        var ad = (await _client.ExtractCompetenciesAsync(Query(minPrediction: 0.7), Ct)).Value.ShouldHaveSingleItem();

        ad.Competencies.Select(c => c.Label).ShouldBe(["C#", "Docker"]);
    }

    [Fact]
    public async Task Extract_SortsByPredictionAndRemovesDuplicateLabels_KeepingTheHighest()
    {
        RespondWith(Body(Document("1", ("sql", 0.85), ("C#", 0.9), ("SQL", 0.95))));

        var ad = (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Value.ShouldHaveSingleItem();

        ad.Competencies.ShouldBe([new ExtractedCompetency("SQL", 0.95), new ExtractedCompetency("C#", 0.9)]);
    }

    [Fact]
    public async Task Extract_SkipsBlankLabels_AndTrims()
    {
        RespondWith(Body(Document("1", (null, 0.99), ("  ", 0.98), (" Java ", 0.9))));

        var ad = (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Value.ShouldHaveSingleItem();

        ad.Competencies.ShouldBe([new ExtractedCompetency("Java", 0.9)]);
    }

    [Fact]
    public async Task Extract_ReturnsAdsInQueryOrder_AndIgnoresUnknownDocuments()
    {
        RespondWith(Body(Real("2"), Real("unknown"), Real("1")));

        var ads = (await _client.ExtractCompetenciesAsync(Query("1", "2"), Ct)).Value;

        ads.Select(a => a.AdId).ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task Extract_DocumentWithoutCandidates_HasNoCompetencies()
    {
        RespondWith("""[{ "doc_id": "1" }]""");

        var ad = (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Value.ShouldHaveSingleItem();

        ad.Competencies.ShouldBeEmpty();
    }

    [Fact]
    public async Task Extract_MissingDocument_FailsWithMissingDocument()
    {
        RespondWith(Body(Real("1")));

        var error = (await _client.ExtractCompetenciesAsync(Query("1", "2"), Ct)).Error;

        error.ShouldBe(EnrichmentsErrors.MissingDocument("2"));
        error.Type.ShouldBe(ErrorType.Unavailable);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(503)]
    [InlineData(400)]
    public async Task Extract_ErrorStatus_FailsAsUnavailable(int status)
    {
        RespondWith("{}", status);

        var error = (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Error;

        error.Code.ShouldBe("Enrichments.Unavailable");
        error.Type.ShouldBe(ErrorType.Unavailable);
        error.Message.ShouldContain(status.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"doc_id":"1"}""")]
    public async Task Extract_UnreadableBody_FailsWithInvalidResponse(string body)
    {
        RespondWith(body);

        (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Error.ShouldBe(EnrichmentsErrors.InvalidResponse);
    }

    [Fact]
    public async Task Extract_Timeout_FailsAsUnavailable()
    {
        _server
            .Given(Request.Create().WithPath("/enrichtextdocuments").UsingPost())
            .RespondWith(Response.Create().WithBody(Body(Real("1"))).WithDelay(TimeSpan.FromSeconds(2)));
        _http.Timeout = TimeSpan.FromMilliseconds(200);

        (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Error.Message.ShouldContain("timeout");
    }

    [Fact]
    public async Task Extract_ServerDown_FailsAsUnavailable()
    {
        _server.Stop();

        (await _client.ExtractCompetenciesAsync(Query("1"), Ct)).Error.Code.ShouldBe("Enrichments.Unavailable");
    }

    [Fact]
    public async Task Extract_CancelledByCaller_Throws()
    {
        RespondWith(Body(Real("1")));
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(
            () => _client.ExtractCompetenciesAsync(Query("1"), cancelled.Token));
    }
}
