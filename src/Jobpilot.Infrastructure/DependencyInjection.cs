using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Infrastructure.JobTech;
using Jobpilot.Infrastructure.JobTech.Enrichments;
using Jobpilot.Infrastructure.JobTech.JobSearch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobpilot.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the JobTech clients. Each gets the standard resilience pipeline
    /// (retries with backoff on 5xx/429/timeouts, attempt and total timeouts, circuit breaker),
    /// tunable under "JobTech:{Api}:Resilience" (optional; defaults apply without it). Enrichments is a POST, but it only analyses
    /// text and has no side effects, so retrying it is safe.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var jobSearch = AddApiOptions<JobSearchOptions>(services, configuration, JobSearchOptions.SectionName);
        services.AddHttpClient<IJobSearchClient, JobSearchClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<JobSearchOptions>>().Value.BaseUrl)
            .AddResilience(jobSearch);

        var enrichments = AddApiOptions<EnrichmentsOptions>(services, configuration, EnrichmentsOptions.SectionName);
        services.AddHttpClient<IEnrichmentsClient, EnrichmentsClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<EnrichmentsOptions>>().Value.BaseUrl)
            .AddResilience(enrichments);

        return services;
    }

    /// <summary>Adds the standard resilience pipeline, tuned by "{api}:Resilience" when that section exists.</summary>
    private static void AddResilience(this IHttpClientBuilder builder, IConfigurationSection api)
    {
        var resilience = api.GetSection("Resilience");
        if (resilience.Exists())
        {
            builder.AddStandardResilienceHandler(resilience);
        }
        else
        {
            builder.AddStandardResilienceHandler();
        }
    }

    /// <summary>Binds and validates the options for one API. Returns its configuration section.</summary>
    private static IConfigurationSection AddApiOptions<TOptions>(
        IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : JobTechApiOptions
    {
        var section = configuration.GetSection(sectionName);

        services.AddOptions<TOptions>()
            .Bind(section)
            .Validate(o => o.BaseUrl is { IsAbsoluteUri: true }, $"{sectionName}:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        return section;
    }
}
