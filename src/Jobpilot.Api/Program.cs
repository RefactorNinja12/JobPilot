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
app.MapProfileEndpoints();

await app.RunAsync();
