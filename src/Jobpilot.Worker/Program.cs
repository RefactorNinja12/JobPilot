using Jobpilot.Application;
using Jobpilot.Infrastructure;
using Jobpilot.Infrastructure.Persistence;
using Jobpilot.Worker;

var builder = Host.CreateApplicationBuilder(args);

// ConnectionStrings:Jobpilot comes from the environment (ConnectionStrings__Jobpilot) or user secrets, never appsettings.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSingleton(TimeProvider.System)
    .AddHostedService<SkillVocabularyRefresher>();

var host = builder.Build();

// The Worker owns the schema: migrate before any background work touches the database.
await host.Services.MigrateDatabaseAsync();
await host.RunAsync();
