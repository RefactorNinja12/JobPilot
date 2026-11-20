using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Testing;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Api.IntegrationTests;

public sealed class DocumentEndpointsTests : IAsyncDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _http;

    public DocumentEndpointsTests()
    {
        var store = new InMemoryBaseDocumentStore();
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host => host.ConfigureTestServices(services =>
            services.AddSingleton<IBaseDocumentStore>(store)));
        _http = _factory.CreateClient();
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    public async ValueTask DisposeAsync()
    {
        _http.Dispose();
        await _factory.DisposeAsync();
    }

    private async Task<HttpResponseMessage> UploadAsync(byte[] bytes, string fileName = "cv.docx")
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(file, "file", fileName);
        return await _http.PostAsync("/api/documents/extract", form, Ct);
    }

    [Fact]
    public async Task Get_NothingSaved_IsAnEmptyList()
    {
        var body = await _http.GetFromJsonAsync<JsonElement>("/api/documents", Ct);

        body.GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Put_SavesTheDocument_AndGetListsItCvFirst()
    {
        (await _http.PutAsJsonAsync("/api/documents", new { kind = "CoverLetter", text = "Hej!" }, Ct)).Dispose();
        using var saved = await _http.PutAsJsonAsync("/api/documents", new { kind = "Cv", text = "  Mitt CV  " }, Ct);

        saved.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = await _http.GetFromJsonAsync<JsonElement>("/api/documents", Ct);
        body.EnumerateArray().Select(d => d.GetProperty("kind").GetString()).ShouldBe(["Cv", "CoverLetter"]);
        body[0].GetProperty("text").GetString().ShouldBe("Mitt CV");
    }

    [Fact]
    public async Task Put_EmptyText_IsA400WithTheErrorCode()
    {
        using var response = await _http.PutAsJsonAsync("/api/documents", new { kind = "Cv", text = " " }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString().ShouldBe("BaseDocument.TextEmpty");
    }

    [Fact]
    public async Task Put_KindAsNumber_IsRejected()
    {
        using var response = await _http.PutAsJsonAsync("/api/documents", new { kind = 1, text = "Mitt CV" }, Ct);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Extract_ReturnsTheTextOfAWordFile_WithoutSavingIt()
    {
        using var response = await UploadAsync(TestDocx.FromParagraphs("Anna Åberg", "Systemutvecklare"));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("text").GetString().ShouldBe("Anna Åberg\nSystemutvecklare");
        (await _http.GetFromJsonAsync<JsonElement>("/api/documents", Ct)).GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Extract_NotAWordFile_IsA400WithTheErrorCode()
    {
        using var response = await UploadAsync("bara text"u8.ToArray(), "cv.txt");

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadFromJsonAsync<JsonElement>(Ct)).GetProperty("code").GetString().ShouldBe("DocxText.NotADocxFile");
    }

    [Fact]
    public async Task Extract_FileOverTheLimit_IsA413()
    {
        using var response = await UploadAsync(new byte[5 * 1024 * 1024 + 1]);

        response.StatusCode.ShouldBe(HttpStatusCode.RequestEntityTooLarge);
    }
}
