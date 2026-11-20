using Jobpilot.Application.Abstractions.Assessments;
using Jobpilot.Application.Abstractions.Documents;
using Jobpilot.Application.Abstractions.Employers;
using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Abstractions.JobSearch;
using Jobpilot.Application.Abstractions.Places;
using Jobpilot.Application.Abstractions.Taxonomy;
using Jobpilot.Application.Abstractions.Usage;
using Jobpilot.Application.Profiles;
using Jobpilot.Infrastructure.Documents;
using Jobpilot.Infrastructure.Employers;
using Jobpilot.Infrastructure.JobTech;
using Jobpilot.Infrastructure.JobTech.Enrichments;
using Jobpilot.Infrastructure.JobTech.JobSearch;
using Jobpilot.Infrastructure.JobTech.Taxonomy;
using Jobpilot.Infrastructure.Persistence;
using Jobpilot.Infrastructure.Persistence.AdCompetencies;
using Jobpilot.Infrastructure.Persistence.Assessments;
using Jobpilot.Infrastructure.Persistence.Documents;
using Jobpilot.Infrastructure.Persistence.Profiles;
using Jobpilot.Infrastructure.Persistence.SkillVocabularies;
using Jobpilot.Infrastructure.Persistence.Usage;
using Jobpilot.Infrastructure.Places;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Jobpilot.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Name of the PostgreSQL connection string ("ConnectionStrings:Jobpilot").</summary>
    public const string ConnectionStringName = "Jobpilot";

    /// <summary>
    /// Registers the JobTech clients. Each gets the standard resilience pipeline
    /// (retries with backoff on 5xx/429/timeouts, attempt and total timeouts, circuit breaker),
    /// tunable under "JobTech:{Api}:Resilience" (optional; defaults apply without it). Enrichments is a POST, but it only analyses
    /// text and has no side effects, so retrying it is safe.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IPlaceDirectory, EmbeddedPlaceDirectory>();

        var jobSearch = AddApiOptions<JobSearchOptions>(services, configuration, JobSearchOptions.SectionName);
        services.AddHttpClient<IJobSearchClient, JobSearchClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<JobSearchOptions>>().Value.BaseUrl)
            .AddResilience(jobSearch);

        var enrichments = AddApiOptions<EnrichmentsOptions>(services, configuration, EnrichmentsOptions.SectionName);
        services.AddHttpClient<IEnrichmentsClient, EnrichmentsClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<EnrichmentsOptions>>().Value.BaseUrl)
            .AddResilience(enrichments);

        // Same API and settings as EnrichmentsClient. The download is ~7 MB and takes ~6 s,
        // within the default 10 s attempt timeout.
        services.AddHttpClient<ISkillVocabularyClient, SkillVocabularyClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<EnrichmentsOptions>>().Value.BaseUrl)
            .AddResilience(enrichments);

        var taxonomy = AddApiOptions<TaxonomyOptions>(services, configuration, TaxonomyOptions.SectionName);
        services.AddHttpClient<IOccupationSearch, TaxonomyClient>((provider, http) =>
                http.BaseAddress = provider.GetRequiredService<IOptions<TaxonomyOptions>>().Value.BaseUrl)
            .AddResilience(taxonomy);

        // A mistyped number would silently stop the filter, so invalid entries fail at start instead.
        services.AddOptions<StaffingAgencyOptions>()
            .Bind(configuration.GetSection(StaffingAgencyOptions.SectionName))
            .Validate(
                o => o.OrganizationNumbers.All(n => ConfiguredStaffingAgencyDirectory.Normalize(n) is not null),
                $"{StaffingAgencyOptions.SectionName}:OrganizationNumbers must be Swedish organization numbers (10 digits).")
            .ValidateOnStart();
        services.AddSingleton<IStaffingAgencyDirectory, ConfiguredStaffingAgencyDirectory>();

        AddPersistence(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers <see cref="JobpilotDbContext"/> on PostgreSQL. The connection string is read when the context is
    /// first created, so hosts that never touch the database start without one.
    /// </summary>
    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<JobpilotDbContext>(options => JobpilotDbContext.Configure(
            options,
            configuration.GetConnectionString(ConnectionStringName)
            ?? throw new InvalidOperationException($"ConnectionStrings:{ConnectionStringName} is not configured.")));

        services.AddScoped<IProfileRepository, ProfileRepository>();
        services.AddScoped<ISkillVocabularyStore, SkillVocabularyStore>();
        services.AddScoped<IAdCompetencyCache, AdCompetencyCache>();
        services.AddScoped<IAssessmentStore, AssessmentStore>();
        services.AddScoped<IBaseDocumentStore, BaseDocumentStore>();
        services.AddSingleton<IDocxTextExtractor, DocxTextExtractor>();
        services.AddScoped<IToolUsageCounter, ToolUsageCounter>();
        services.TryAddSingleton(TimeProvider.System);
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
