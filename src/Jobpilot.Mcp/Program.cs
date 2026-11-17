using Jobpilot.Application;
using Jobpilot.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ConnectionStrings:Jobpilot: appsettings.Development.json points at the local compose database;
// elsewhere it comes from the environment (ConnectionStrings__Jobpilot).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

// Streamable HTTP, stateless by default in this SDK version: no session affinity needed in the cluster.
// Reached only over Tailscale; there is no authentication in the app itself yet.
builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok("healthy"));
app.MapMcp("/mcp");

await app.RunAsync();
