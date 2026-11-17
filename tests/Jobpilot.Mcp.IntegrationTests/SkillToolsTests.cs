using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Jobpilot.Mcp.IntegrationTests;

/// <summary>Talks to the real MCP server (in memory) through the SDK's own HTTP client.</summary>
public sealed class SkillToolsTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly InMemoryProfileRepository _profiles = new();
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

        tools.Select(t => t.Name).ShouldBe(["search_skills", "add_skill", "update_skill", "remove_skill", "get_profile"], ignoreOrder: true);
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
    public async Task Health_IsOk()
    {
        using var http = _factory.CreateClient();

        (await http.GetAsync(new Uri("/health", UriKind.Relative), Ct)).IsSuccessStatusCode.ShouldBeTrue();
    }
}
