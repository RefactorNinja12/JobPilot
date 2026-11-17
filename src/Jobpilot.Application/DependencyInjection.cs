using Jobpilot.Application.Jobs.FindJobs;
using Jobpilot.Application.Jobs.GetJobAd;
using Jobpilot.Application.Jobs.SaveAssessment;
using Jobpilot.Application.Occupations.Search;
using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.RemoveSkill;
using Jobpilot.Application.Profiles.SetOccupations;
using Jobpilot.Application.Profiles.SetPreferences;
using Jobpilot.Application.Profiles.UpdateSkill;
using Jobpilot.Application.SkillVocabularies.Refresh;
using Jobpilot.Application.SkillVocabularies.Search;
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
        services.AddScoped<GetProfileHandler>();
        services.AddScoped<UpdateSkillHandler>();
        services.AddScoped<RemoveSkillHandler>();
        services.AddScoped<SetPreferencesHandler>();
        services.AddScoped<SetOccupationsHandler>();
        services.AddScoped<SearchOccupationsHandler>();
        services.AddScoped<FindJobsHandler>();
        services.AddScoped<GetJobAdHandler>();
        services.AddScoped<SaveAssessmentHandler>();
        services.AddScoped<RefreshSkillVocabularyHandler>();
        services.AddScoped<SearchSkillsHandler>();
        return services;
    }
}
