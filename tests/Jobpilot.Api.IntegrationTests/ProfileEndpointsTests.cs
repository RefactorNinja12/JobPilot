using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class ProfileEndpointsTests : IAsyncDisposable
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public ProfileEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
            services.AddSingleton<IProfileRepository>(_profiles)));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetProfile_BeforeAnything_IsEmptyWithNoDealbreakers()
    {
        var profile = await _http.GetFromJsonAsync<JsonElement>("/api/profile", Ct);

        profile.GetProperty("skills").GetArrayLength().ShouldBe(0);
        profile.GetProperty("targetOccupations").GetArrayLength().ShouldBe(0);
        profile.GetProperty("preferences").GetProperty("acceptsPartTime").GetBoolean().ShouldBeTrue();
        profile.GetProperty("preferences").GetProperty("commute").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task GetProfile_ReturnsSkillsOccupationsAndPreferences()
    {
        var stored = Profile.Create();
        stored.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert, 8).Value);
        stored.SetTargetOccupations([Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value]);
        stored.UpdatePreferences(JobPreferences.Create(
            acceptsFullTime: true,
            acceptsPartTime: false,
            excludeStaffingAgencies: true,
            CommuteLimit.Create(Coordinates.Create(57.7089, 11.9746).Value, 40).Value).Value);
        _profiles.Add(stored);

        var profile = await _http.GetFromJsonAsync<JsonElement>("/api/profile", Ct);

        var skill = profile.GetProperty("skills").EnumerateArray().ShouldHaveSingleItem();
        skill.GetProperty("name").GetString().ShouldBe("C#");
        skill.GetProperty("level").GetString().ShouldBe("Expert");
        skill.GetProperty("yearsOfExperience").GetInt32().ShouldBe(8);
        profile.GetProperty("targetOccupations")[0].GetProperty("label").GetString().ShouldBe("Backend-utvecklare");
        var preferences = profile.GetProperty("preferences");
        preferences.GetProperty("excludeStaffingAgencies").GetBoolean().ShouldBeTrue();
        preferences.GetProperty("commute").GetProperty("maxDistanceKm").GetInt32().ShouldBe(40);
    }
}
