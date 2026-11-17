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

var app = builder.Build();

app.UseExceptionHandler();
app.MapGet("/health", () => Results.Ok("healthy"));
app.MapJobEndpoints();
app.MapProfileEndpoints();

await app.RunAsync();
