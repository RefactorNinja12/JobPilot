using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Application.SkillVocabularies.Refresh;
using Microsoft.Extensions.DependencyInjection;

namespace Jobpilot.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the use case handlers. Scoped, like the repositories they use: one per request or job run.
    /// Hosts call this together with AddInfrastructure.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AddSkillHandler>();
        services.AddScoped<RefreshSkillVocabularyHandler>();
        return services;
    }
}
