using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Infrastructure.JobTech.JobSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobpilot.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the JobTech clients. JobSearch calls get the standard resilience pipeline
    /// (retries with backoff on 5xx/429/timeouts, attempt and total timeouts, circuit breaker),
    /// tunable under "JobTech:JobSearch:Resilience".
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(JobSearchOptions.SectionName);

        services.AddOptions<JobSearchOptions>()
            .Bind(section)
            .Validate(o => o.BaseUrl is { IsAbsoluteUri: true }, $"{JobSearchOptions.SectionName}:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddHttpClient<IJobSearchClient, JobSearchClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<JobSearchOptions>>().Value.BaseUrl)
            .AddStandardResilienceHandler(section.GetSection("Resilience"));

        return services;
    }
}
