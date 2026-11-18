using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class PreferenceEndpointsTests : IAsyncDisposable
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public PreferenceEndpointsTests()
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

    private static object Goteborg(int km) => new { homeLatitude = 57.7089, homeLongitude = 11.9746, maxDistanceKm = km };

    private Task<HttpResponseMessage> SetAsync(bool fullTime = true, bool partTime = false, bool noAgencies = true, object? commute = null) =>
        _http.PutAsJsonAsync(
            "/api/profile/preferences",
            new { acceptsFullTime = fullTime, acceptsPartTime = partTime, excludeStaffingAgencies = noAgencies, commute },
            Ct);

    private JobPreferences Stored() => _profiles.Profile.ShouldNotBeNull().Preferences;

    private static async Task<string?> CodeAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString();

    [Fact]
    public async Task Set_StoresAllDealbreakers_AndAnswersInTheProfileShape()
    {
        using var response = await SetAsync(commute: Goteborg(40));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);
        body.GetProperty("commute").GetProperty("maxDistanceKm").GetInt32().ShouldBe(40);
        Stored().AcceptsPartTime.ShouldBeFalse();
        Stored().ExcludeStaffingAgencies.ShouldBeTrue();
        Stored().CommuteLimit.ShouldNotBeNull().MaxDistanceKm.ShouldBe(40);
    }

    [Fact]
    public async Task Set_NullCommute_RemovesTheLimit()
    {
        (await SetAsync(commute: Goteborg(40))).Dispose();

        using var response = await SetAsync(commute: null);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        Stored().CommuteLimit.ShouldBeNull();
    }

    [Fact]
    public async Task Set_NeitherFullTimeNorPartTime_Is400()
    {
        using var response = await SetAsync(fullTime: false, partTime: false);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CodeAsync(response)).ShouldBe("JobPreferences.NoEmploymentExtentAccepted");
        _profiles.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task Set_DistanceOutOfRange_Is400()
    {
        using var response = await SetAsync(commute: Goteborg(501));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CodeAsync(response)).ShouldBe("CommuteLimit.DistanceOutOfRange");
    }

    [Fact]
    public async Task Set_LatitudeOutOfRange_Is400()
    {
        using var response = await SetAsync(commute: new { homeLatitude = 91.0, homeLongitude = 11.9746, maxDistanceKm = 40 });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await CodeAsync(response)).ShouldBe("Coordinates.LatitudeOutOfRange");
    }
}
