using Jobpilot.Domain.Common;
using Jobpilot.Infrastructure.JobTech.Enrichments;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace Jobpilot.Infrastructure.IntegrationTests.JobTech.Enrichments;

/// <summary>Runs the real vocabulary client against WireMock standing in for GET /synonymdictionary.</summary>
public sealed class SkillVocabularyClientTests : IDisposable
{
    /// <summary>Excerpt of the real response (65,512 items), captured 2026-11-17.</summary>
    private const string RealBody = """
        {
          "total": 65512,
          "build_datetime": "2025-06-17T10:01:51",
          "items": [
            { "concept": "Kafka", "term": "apache kafka", "term_misspelled": false, "type": "KOMPETENS" },
            { "concept": "Kafka", "term": "kafka", "term_misspelled": false, "type": "KOMPETENS" },
            { "concept": "C#", "term": "c #", "term_misspelled": false, "type": "KOMPETENS" },
            { "concept": "C#", "term": "c#", "term_misspelled": false, "type": "KOMPETENS" },
            { "concept": "C#", "term": "c-sharp", "term_misspelled": false, "type": "KOMPETENS" },
            { "concept": "C#", "term": "csharp", "term_misspelled": false, "type": "KOMPETENS" }
          ]
        }
        """;

    private readonly WireMockServer _server = WireMockServer.Start();
    private readonly HttpClient _http;
    private readonly SkillVocabularyClient _client;

    public SkillVocabularyClientTests()
    {
        _http = new HttpClient { BaseAddress = new Uri(_server.Url! + "/") };
        _client = new SkillVocabularyClient(_http);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public void Dispose()
    {
        _http.Dispose();
        _server.Dispose();
    }

    private void RespondWith(string body, int status = 200) =>
        _server
            .Given(Request.Create().WithPath("/synonymdictionary").UsingGet())
            .RespondWith(Response.Create().WithStatusCode(status).WithBody(body));

    [Fact]
    public async Task Get_AsksForCorrectlySpelledCompetencies()
    {
        RespondWith(RealBody);

        await _client.GetCompetenciesAsync(Ct);

        var query = _server.LogEntries.ShouldHaveSingleItem().RequestMessage!.Query!;
        query["type"].ShouldBe(["COMPETENCE"]);
        query["spelling"].ShouldBe(["CORRECTLY_SPELLED"]);
    }

    [Fact]
    public async Task Get_GroupsTermsByConcept()
    {
        RespondWith(RealBody);

        var vocabulary = (await _client.GetCompetenciesAsync(Ct)).Value;

        vocabulary.Count.ShouldBe(2);
        vocabulary.Find("apache kafka")!.Name.ShouldBe("Kafka");
        vocabulary.Find("c-sharp")!.Terms.ShouldBe(["c #", "c#", "c-sharp", "csharp"]);
    }

    [Theory]
    [InlineData("2025-06-17T10:01:51", 2)] // Summer time, CEST
    [InlineData("2025-01-17T10:01:51", 1)] // Winter time, CET
    public async Task Get_ReadsBuildTimeAsSwedishLocalTime(string buildDatetime, int offsetHours)
    {
        RespondWith($$"""{ "build_datetime": "{{buildDatetime}}", "items": [] }""");

        var builtAt = (await _client.GetCompetenciesAsync(Ct)).Value.BuiltAt;

        builtAt.ShouldBe(new DateTimeOffset(DateTime.Parse(buildDatetime, System.Globalization.CultureInfo.InvariantCulture), TimeSpan.FromHours(offsetHours)));
    }

    [Fact]
    public async Task Get_SkipsItemsWithoutConceptOrTerm()
    {
        RespondWith("""
            {
              "build_datetime": "2025-06-17T10:01:51",
              "items": [
                { "term": "orphan" },
                { "concept": "Kafka" },
                { "concept": "Kafka", "term": "kafka" }
              ]
            }
            """);

        var vocabulary = (await _client.GetCompetenciesAsync(Ct)).Value;

        vocabulary.Count.ShouldBe(1);
        vocabulary.Find("Kafka")!.Terms.ShouldBe(["kafka"]);
        vocabulary.Find("orphan").ShouldBeNull();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("""{ "items": [] }""")]
    [InlineData("""{ "build_datetime": "2025-06-17T10:01:51" }""")]
    public async Task Get_UnreadableOrIncompleteBody_FailsWithInvalidResponse(string body)
    {
        RespondWith(body);

        (await _client.GetCompetenciesAsync(Ct)).Error.ShouldBe(EnrichmentsErrors.InvalidResponse);
    }

    [Theory]
    [InlineData(500)]
    [InlineData(404)]
    public async Task Get_ErrorStatus_FailsAsUnavailable(int status)
    {
        RespondWith("{}", status);

        var error = (await _client.GetCompetenciesAsync(Ct)).Error;

        error.Code.ShouldBe("Enrichments.Unavailable");
        error.Type.ShouldBe(ErrorType.Unavailable);
    }

    [Fact]
    public async Task Get_Timeout_FailsAsUnavailable()
    {
        _server
            .Given(Request.Create().WithPath("/synonymdictionary").UsingGet())
            .RespondWith(Response.Create().WithBody(RealBody).WithDelay(TimeSpan.FromSeconds(2)));
        _http.Timeout = TimeSpan.FromMilliseconds(200);

        (await _client.GetCompetenciesAsync(Ct)).Error.Message.ShouldContain("timeout");
    }

    [Fact]
    public async Task Get_ServerDown_FailsAsUnavailable()
    {
        _server.Stop();

        (await _client.GetCompetenciesAsync(Ct)).Error.Code.ShouldBe("Enrichments.Unavailable");
    }

    [Fact]
    public async Task Get_CancelledByCaller_Throws()
    {
        RespondWith(RealBody);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _client.GetCompetenciesAsync(cancelled.Token));
    }
}
