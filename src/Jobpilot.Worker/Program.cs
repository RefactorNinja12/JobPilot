using Jobpilot.Application;
using Jobpilot.Infrastructure;
using Jobpilot.Worker;

var builder = Host.CreateApplicationBuilder(args);

// ConnectionStrings:Jobpilot comes from the environment (ConnectionStrings__Jobpilot) or user secrets, never appsettings.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddSingleton(TimeProvider.System)
    .AddHostedService<SkillVocabularyRefresher>();

await builder.Build().RunAsync();
