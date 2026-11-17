using Jobpilot.Application;
using Jobpilot.Infrastructure;
using Jobpilot.Infrastructure.Persistence;
using Jobpilot.Worker;

var builder = Host.CreateApplicationBuilder(args);

// ConnectionStrings:Jobpilot: appsettings.Development.json points at the local compose database;
// elsewhere it comes from the environment (ConnectionStrings__Jobpilot).
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddHostedService<SkillVocabularyRefresher>();

var host = builder.Build();

// The Worker owns the schema: migrate before any background work touches the database.
await host.Services.MigrateDatabaseAsync();
await host.RunAsync();
