using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Application.Profiles;
using Jobpilot.Domain.Profiles;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class OccupationEndpointsTests : IAsyncDisposable
{
    private readonly InMemoryProfileRepository _profiles = new();
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public OccupationEndpointsTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
        {
            services.AddSingleton<IProfileRepository>(_profiles);
            services.AddSingleton<IOccupationSearch>(new FakeOccupationSearch());
        }));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    private Task<HttpResponseMessage> SetAsync(params object[] occupations) =>
        _http.PutAsJsonAsync("/api/profile/occupations", occupations, Ct);

    [Fact]
    public async Task Set_ReplacesTheList()
    {
        (await SetAsync(new { taxonomyId = "7wdX_4rv_33z", label = "Backend-utvecklare" })).Dispose();

        using var response = await SetAsync(new { taxonomyId = "71Ji_irM_rSJ", label = "Fullstack-utvecklare" });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var stored = _profiles.Profile.ShouldNotBeNull().TargetOccupations.ShouldHaveSingleItem();
        stored.TaxonomyId.ShouldBe("71Ji_irM_rSJ");
        stored.Label.ShouldBe("Fullstack-utvecklare");
    }

    [Fact]
    public async Task Set_EmptyList_ClearsThem()
    {
        (await SetAsync(new { taxonomyId = "7wdX_4rv_33z", label = "Backend-utvecklare" })).Dispose();

        using var response = await SetAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        _profiles.Profile.ShouldNotBeNull().TargetOccupations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_TooMany_Is400()
    {
        var eleven = Enumerable.Range(1, Profile.MaxTargetOccupations + 1)
            .Select(i => (object)new { taxonomyId = string.Create(CultureInfo.InvariantCulture, $"id{i}"), label = string.Create(CultureInfo.InvariantCulture, $"Yrke {i}") })
            .ToArray();

        using var response = await SetAsync(eleven);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString()
            .ShouldBe("Profile.TooManyTargetOccupations");
    }

    [Fact]
    public async Task Set_BlankLabel_Is400()
    {
        using var response = await SetAsync(new { taxonomyId = "7wdX_4rv_33z", label = " " });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        _profiles.Profile.ShouldBeNull();
    }

    [Fact]
    public async Task Search_FindsOccupationsByName()
    {
        var found = await _http.GetFromJsonAsync<JsonElement>("/api/occupations/search?q=backend", Ct);

        var occupation = found.EnumerateArray().ShouldHaveSingleItem();
        occupation.GetProperty("taxonomyId").GetString().ShouldBe("7wdX_4rv_33z");
        occupation.GetProperty("label").GetString().ShouldBe("Backend-utvecklare");
    }

    [Fact]
    public async Task Search_MaxOutOfRange_Is400()
    {
        using var response = await _http.GetAsync(new Uri("/api/occupations/search?q=dev&max=100", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
