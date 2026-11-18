using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jobpilot.Api.IntegrationTests;

public sealed class PlaceEndpointsTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly HttpClient _http;

    public PlaceEndpointsTests() => _http = _factory.CreateClient();

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Search_FindsPlacesByName_WithCoordinates()
    {
        var found = await _http.GetFromJsonAsync<JsonElement>("/api/places/search?q=kvissleby", Ct);

        var place = found.EnumerateArray().First();
        place.GetProperty("name").GetString().ShouldBe("Kvissleby");
        place.GetProperty("latitude").GetDouble().ShouldBeInRange(62.0, 63.0);
        place.GetProperty("longitude").GetDouble().ShouldBeInRange(16.0, 18.0);
    }

    [Fact]
    public async Task Search_NoText_IsEmpty()
    {
        var found = await _http.GetFromJsonAsync<JsonElement>("/api/places/search", Ct);

        found.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Search_MaxOutOfRange_Is400()
    {
        using var response = await _http.GetAsync(new Uri("/api/places/search?q=sto&max=100", UriKind.Relative), Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
