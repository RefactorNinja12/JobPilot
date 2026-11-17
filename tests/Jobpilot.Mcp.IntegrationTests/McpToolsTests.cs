using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>
/// Talks to the real MCP server (in memory) through the SDK's own HTTP client. Storage and the JobTech APIs are fakes.
/// </summary>
public sealed class McpToolsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly InMemoryProfileRepository _profiles = new();
    private readonly FakeJobSearch _jobSearch = new();
    private readonly FakeEnrichments _enrichments = new();
    private readonly InMemorySkillVocabularyStore _vocabularies = new()
    {
        Vocabulary = SkillVocabulary.Create(
            new DateTimeOffset(2025, 6, 17, 10, 1, 51, TimeSpan.FromHours(2)),
            [
                new SkillConcept("Kafka", ["kafka", "apache kafka"]),
                new SkillConcept("C#", ["c#", "c-sharp"]),
            ]),
    };

    private WebApplicationFactory<Program> _factory = null!;
    private McpClient _client = null!;

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProfileRepository>(_profiles);
            services.AddSingleton<ISkillVocabularyStore>(_vocabularies);
            services.AddSingleton<Application.Abstractions.Taxonomy.IOccupationSearch>(new FakeOccupationSearch());
            services.AddSingleton<Application.Abstractions.JobSearch.IJobSearchClient>(_jobSearch);
            services.AddSingleton<Application.Abstractions.Enrichments.IEnrichmentsClient>(_enrichments);
        }));

        var transport = new HttpClientTransport(
            new HttpClientTransportOptions { Endpoint = new Uri(_factory.Server.BaseAddress, "mcp") },
            _factory.CreateClient(),
            loggerFactory: null,
            ownsHttpClient: true);
        _client = await McpClient.CreateAsync(transport, cancellationToken: Ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private Task<CallToolResult> CallAsync(string tool, Dictionary<string, object?> arguments) =>
        _client.CallToolAsync(tool, arguments, cancellationToken: Ct).AsTask();

    private static string Text(CallToolResult result) =>
        result.Content.OfType<TextContentBlock>().ShouldHaveSingleItem().Text;

    private static T Read<T>(CallToolResult result)
    {
        result.IsError.ShouldNotBe(expected: true, customMessage: Text(result));
        return JsonSerializer.Deserialize<T>(Text(result), _json)!;
    }

    private sealed record Match(string Name, string[] Synonyms);

    private sealed record Added(string Name, bool InVocabulary);

    [Fact]
    public async Task Server_ListsTheTools()
    {
        var tools = await _client.ListToolsAsync(cancellationToken: Ct);

        tools.Select(t => t.Name).ShouldBe(["search_skills", "add_skill", "update_skill", "remove_skill", "get_profile", "set_preferences", "search_occupations", "set_occupations", "find_jobs", "get_job_ad"], ignoreOrder: true);
    }

    [Fact]
    public async Task SearchSkills_FindsBySynonym()
    {
        var result = await CallAsync("search_skills", new(StringComparer.Ordinal) { ["text"] = "apache" });

        Read<Match[]>(result).ShouldHaveSingleItem().ShouldBeEquivalentTo(new Match("Kafka", ["kafka", "apache kafka"]));
    }

    [Fact]
    public async Task SearchSkills_WithoutVocabulary_IsAToolError()
    {
        _vocabularies.Vocabulary = null;

        var result = await CallAsync("search_skills", new(StringComparer.Ordinal) { ["text"] = "kafka" });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("SearchSkills.VocabularyNotLoaded");
    }

    [Fact]
    public async Task AddSkill_StoresTheConceptNameInTheProfile()
    {
        var result = await CallAsync("add_skill", new(StringComparer.Ordinal)
        {
            ["name"] = "apache kafka",
            ["level"] = "expert",
            ["yearsOfExperience"] = 5,
        });

        Read<Added>(result).ShouldBe(new Added("Kafka", InVocabulary: true));
        var skill = _profiles.Profile.ShouldNotBeNull().Skills.ShouldHaveSingleItem();
        skill.Skill.Name.ShouldBe("Kafka");
        skill.YearsOfExperience.ShouldBe(5);
    }

    [Theory]
    [InlineData("Guru")]
    [InlineData("3")]
    public async Task AddSkill_UnknownLevel_IsAToolError(string level)
    {
        var result = await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "C#", ["level"] = level, ["yearsOfExperience"] = 1 });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("Unknown level");
        _profiles.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task AddSkill_Twice_IsAToolErrorWithTheDomainCode()
    {
        await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "c#", ["level"] = "Advanced", ["yearsOfExperience"] = 3 });

        var result = await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "c-sharp", ["level"] = "Advanced", ["yearsOfExperience"] = 3 });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("Profile.SkillAlreadyAdded");
    }

    [Fact]
    public async Task GetProfile_BeforeAnySkill_IsEmpty()
    {
        var profile = Read<JsonElement>(await CallAsync("get_profile", []));

        profile.GetProperty("skills").GetArrayLength().ShouldBe(0);
        profile.GetProperty("preferences").GetProperty("acceptsFullTime").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    public async Task GetProfile_ShowsAddedSkills()
    {
        await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "apache kafka", ["level"] = "Advanced", ["yearsOfExperience"] = 3 });

        var skill = Read<JsonElement>(await CallAsync("get_profile", [])).GetProperty("skills").EnumerateArray().ShouldHaveSingleItem();

        skill.GetProperty("name").GetString().ShouldBe("Kafka");
        skill.GetProperty("level").GetString().ShouldBe("Advanced");
        skill.GetProperty("yearsOfExperience").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task UpdateSkill_BySynonym_ChangesTheStoredSkill()
    {
        await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "Kafka", ["level"] = "Beginner", ["yearsOfExperience"] = 1 });

        var result = await CallAsync("update_skill", new(StringComparer.Ordinal) { ["name"] = "apache kafka", ["level"] = "Expert", ["yearsOfExperience"] = 4 });

        result.IsError.ShouldNotBe(expected: true, customMessage: Text(result));
        var skill = _profiles.Profile.ShouldNotBeNull().Skills.ShouldHaveSingleItem();
        skill.Level.ShouldBe(Domain.Profiles.SkillLevel.Expert);
        skill.YearsOfExperience.ShouldBe(4);
    }

    [Fact]
    public async Task RemoveSkill_RemovesItAndReturnsTheStoredName()
    {
        await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "Kafka", ["level"] = "Beginner", ["yearsOfExperience"] = 1 });

        var result = await CallAsync("remove_skill", new(StringComparer.Ordinal) { ["name"] = "apache kafka" });

        Text(result).ShouldContain("Kafka");
        _profiles.Profile.ShouldNotBeNull().Skills.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveSkill_NotInProfile_IsAToolError()
    {
        var result = await CallAsync("remove_skill", new(StringComparer.Ordinal) { ["name"] = "Kafka" });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("Profile.SkillNotFound");
    }

    [Fact]
    public async Task SetPreferences_WithCommute_IsStoredInTheProfile()
    {
        var result = await CallAsync("set_preferences", new(StringComparer.Ordinal)
        {
            ["acceptsFullTime"] = true,
            ["acceptsPartTime"] = false,
            ["excludeStaffingAgencies"] = true,
            ["homeLatitude"] = 57.7089,
            ["homeLongitude"] = 11.9746,
            ["maxDistanceKm"] = 40,
        });

        result.IsError.ShouldNotBe(expected: true, customMessage: Text(result));
        var preferences = _profiles.Profile.ShouldNotBeNull().Preferences;
        preferences.AcceptsPartTime.ShouldBeFalse();
        preferences.CommuteLimit.ShouldNotBeNull().MaxDistanceKm.ShouldBe(40);
    }

    [Fact]
    public async Task SetPreferences_PartialCommute_IsAToolError()
    {
        var result = await CallAsync("set_preferences", new(StringComparer.Ordinal)
        {
            ["acceptsFullTime"] = true,
            ["acceptsPartTime"] = true,
            ["excludeStaffingAgencies"] = false,
            ["maxDistanceKm"] = 40,
        });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("together");
        _profiles.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task SearchOccupations_ThenSetOccupations_ShowsThemInTheProfile()
    {
        var found = Read<JsonElement>(await CallAsync("search_occupations", new(StringComparer.Ordinal) { ["text"] = "backend" }));
        var backend = found.EnumerateArray().ShouldHaveSingleItem();

        var result = await CallAsync("set_occupations", new(StringComparer.Ordinal) { ["occupations"] = new[] { backend } });

        result.IsError.ShouldNotBe(expected: true, customMessage: Text(result));
        var occupation = Read<JsonElement>(await CallAsync("get_profile", []))
            .GetProperty("targetOccupations").EnumerateArray().ShouldHaveSingleItem();
        occupation.GetProperty("taxonomyId").GetString().ShouldBe("7wdX_4rv_33z");
        occupation.GetProperty("label").GetString().ShouldBe("Backend-utvecklare");
    }

    [Fact]
    public async Task FindJobs_ReturnsRankedMatchesForTheProfile()
    {
        await CallAsync("add_skill", new(StringComparer.Ordinal) { ["name"] = "C#", ["level"] = "Expert", ["yearsOfExperience"] = 8 });
        await CallAsync("set_occupations", new(StringComparer.Ordinal)
        {
            ["occupations"] = new[] { new { taxonomyId = "7wdX_4rv_33z", label = "Backend-utvecklare" } },
        });
        _jobSearch.Ads.AddRange([JobAd("java"), JobAd("csharp")]);
        _enrichments.SkillsByAd["java"] = ["Java"];
        _enrichments.SkillsByAd["csharp"] = ["C#", "Kafka"];

        var result = Read<JsonElement>(await CallAsync("find_jobs", new(StringComparer.Ordinal) { ["top"] = 1 }));

        result.GetProperty("fetched").GetInt32().ShouldBe(2);
        var match = result.GetProperty("matches").EnumerateArray().ShouldHaveSingleItem();
        match.GetProperty("adId").GetString().ShouldBe("csharp");
        match.GetProperty("fitScore").GetInt32().ShouldBe(50);
        match.GetProperty("matchedSkills")[0].GetString().ShouldBe("C#");
        match.GetProperty("missingSkills")[0].GetString().ShouldBe("Kafka");
    }

    [Fact]
    public async Task FindJobs_WithoutOccupations_IsAToolErrorPointingToSetOccupations()
    {
        var result = await CallAsync("find_jobs", []);

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("FindJobs.NoOccupations");
        Text(result).ShouldContain("set_occupations");
    }

    [Fact]
    public async Task GetJobAd_ReturnsTheFullText()
    {
        _jobSearch.Ads.Add(JobAd("31744367"));

        var ad = Read<JsonElement>(await CallAsync("get_job_ad", new(StringComparer.Ordinal) { ["adId"] = "31744367" }));

        ad.GetProperty("adId").GetString().ShouldBe("31744367");
        ad.GetProperty("description").GetString().ShouldBe("Text");
    }

    [Fact]
    public async Task GetJobAd_Unknown_IsAToolError()
    {
        var result = await CallAsync("get_job_ad", new(StringComparer.Ordinal) { ["adId"] = "0000000" });

        result.IsError.ShouldBe(expected: true);
        Text(result).ShouldContain("JobSearch.AdNotFound");
    }

    private static Domain.Jobs.JobAd JobAd(string id) =>
        Domain.Jobs.JobAd.Create(id, "Utvecklare", "Text", new Domain.Jobs.JobAdDetails(
            new Uri("https://arbetsformedlingen.se/platsbanken/annonser/" + id),
            Domain.Jobs.Employer.Create("Arbetsgivare", "2021005448").Value,
            Domain.Jobs.Workplace.Unknown,
            Domain.Jobs.Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value,
            Domain.Jobs.WorkingHours.FullTime,
            new DateTimeOffset(2026, 11, 16, 9, 0, 0, TimeSpan.FromHours(1)),
            new DateOnly(2026, 12, 1))).Value;

    [Fact]
    public async Task Health_IsOk()
    {
        using var http = _factory.CreateClient();

        (await http.GetAsync(new Uri("/health", UriKind.Relative), Ct)).IsSuccessStatusCode.ShouldBeTrue();
    }
}
