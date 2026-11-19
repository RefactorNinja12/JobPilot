using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class SkillEndpointsTests : IAsyncDisposable
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public SkillEndpointsTests()
    {
        var vocabularies = new InMemorySkillVocabularyStore
        {
            Vocabulary = SkillVocabulary.Create(
                DateTimeOffset.UnixEpoch,
                [new SkillConcept("Kafka", ["kafka", "apache kafka"]), new SkillConcept("C#", ["c#", "c-sharp"])]),
        };
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProfileRepository>(_profiles);
            services.AddSingleton<ISkillVocabularyStore>(vocabularies);
        }));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    private Task<HttpResponseMessage> AddAsync(string name, string level = "Advanced") =>
        _http.PostAsJsonAsync("/api/profile/skills", new { name, level }, Ct);

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString();

    private ProfileSkill StoredSkill() => _profiles.Profile.ShouldNotBeNull().Skills.ShouldHaveSingleItem();

    [Fact]
    public async Task Add_BySynonym_StoresTheConceptName()
    {
        using var response = await AddAsync("apache kafka", "Expert");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var added = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        added.GetProperty("name").GetString().ShouldBe("Kafka");
        added.GetProperty("inVocabulary").GetBoolean().ShouldBeTrue();
        StoredSkill().Level.ShouldBe(SkillLevel.Expert);
    }

    [Fact]
    public async Task Add_Duplicate_Is409()
    {
        (await AddAsync("Kafka")).Dispose();

        using var response = await AddAsync("apache kafka");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await CodeAsync(response)).ShouldBe("Profile.SkillAlreadyAdded");
    }

    [Fact]
    public async Task Add_LevelAsNumber_IsRejected()
    {
        using var response = await _http.PostAsJsonAsync(
            "/api/profile/skills", new { name = "Kafka", level = 3 }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _profiles.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task Update_ChangesTheLevel()
    {
        (await AddAsync("Kafka", "Beginner")).Dispose();

        using var response = await _http.PutAsJsonAsync(
            "/api/profile/skills", new { name = "kafka", level = "Expert" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        StoredSkill().Level.ShouldBe(SkillLevel.Expert);
    }

    [Fact]
    public async Task Update_Unknown_Is404()
    {
        using var response = await _http.PutAsJsonAsync(
            "/api/profile/skills", new { name = "Rust", level = "Expert" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await CodeAsync(response)).ShouldBe("Profile.SkillNotFound");
    }

    [Fact]
    public async Task Delete_ByNameWithSpecialCharacters_RemovesIt()
    {
        (await AddAsync("C#")).Dispose();

        using var response = await _http.DeleteAsync(new Uri("/api/profile/skills?name=" + Uri.EscapeDataString("C#"), UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _profiles.Profile.ShouldNotBeNull().Skills.ShouldBeEmpty();
    }

    [Fact]
    public async Task Delete_Unknown_Is404()
    {
        using var response = await _http.DeleteAsync(new Uri("/api/profile/skills?name=Rust", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_SuggestsConceptsBySynonym()
    {
        var found = await _http.GetFromJsonAsync<JsonElement>("/api/skills/search?q=apache", Ct);

        found.EnumerateArray().ShouldHaveSingleItem().GetProperty("name").GetString().ShouldBe("Kafka");
    }
}
