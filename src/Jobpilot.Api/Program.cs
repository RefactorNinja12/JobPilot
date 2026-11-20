using System.Text.Json.Serialization;
using Jobpilot.Api.Endpoints;
using Jobpilot.Application;
using Jobpilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The HTTP API behind the web UI. It calls the same Application handlers as the MCP server, but never an AI:
// browsing jobs, the profile and saved assessments costs no tokens.
// ConnectionStrings:Jobpilot: appsettings.Development.json points at the local compose database;
// elsewhere it comes from the environment (ConnectionStrings__Jobpilot).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddProblemDetails();

// Skill levels travel as names ("Expert"), never as numbers.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

var app = builder.Build();

// Unexpected exceptions become a 500 ProblemDetails; a malformed request (e.g. a level sent as a number) keeps its 400.
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException bad ? bad.StatusCode : StatusCodes.Status500InternalServerError,
});
app.MapGet("/health", () => Results.Ok("healthy"));
app.MapJobEndpoints();
app.MapDocumentEndpoints();
app.MapProfileEndpoints();

// The built web UI (web/dist, copied to wwwroot in the image) is served from the same address as the API, so the UI
// only ever calls its own /api/. In development wwwroot is absent and Vite serves the UI instead.
// index.html is never cached, so a redeploy is picked up at once; the hashed assets it points at may be.
var webUi = new StaticFileOptions
{
    OnPrepareResponse = context =>
    {
        if (context.File.Name == "index.html")
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
    },
};
app.UseStaticFiles(webUi);

// Unknown API paths stay a 404; every other path is a page of the single-page app.
app.MapFallback("/api/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html", webUi);

await app.RunAsync();
