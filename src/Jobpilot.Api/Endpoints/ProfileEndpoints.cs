using Jobpilot.Application.Occupations.Search;
using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.RemoveSkill;
using Jobpilot.Application.Profiles.SetOccupations;
using Jobpilot.Application.Profiles.UpdateSkill;
using Jobpilot.Application.SkillVocabularies.Search;

namespace Jobpilot.Api.Endpoints;

internal static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/api/profile");

        // Skills, target occupations and dealbreakers; an empty profile before anything is added.
        profile.MapGet("/", async (GetProfileHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        // The same use cases Claude calls over MCP: names and synonyms resolve to vocabulary concepts.
        // Skill names go in the body or the query, not the path: "C#" and "Ci/cd" do not survive a path segment.
        profile.MapPost("/skills", async (AddSkillHandler handler, AddSkillCommand command, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(command, cancellationToken)).ToHttpResult());

        profile.MapPut("/skills", async (UpdateSkillHandler handler, UpdateSkillCommand command, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(command, cancellationToken)).ToHttpResult());

        profile.MapDelete("/skills", async (RemoveSkillHandler handler, string name, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(name, cancellationToken)).ToHttpResult());

        // Suggestions while typing a skill name.
        app.MapGet("/api/skills/search", async (SearchSkillsHandler handler, string? q, int? max, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new SearchSkillsQuery(q, max ?? 8), cancellationToken)).ToHttpResult());

        // Target occupations: the whole list is replaced, as with set_occupations over MCP.
        profile.MapPut("/occupations", async (SetOccupationsHandler handler, OccupationView[] occupations, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(occupations, cancellationToken)).ToHttpResult());

        // Suggestions from the Taxonomy occupation list while typing a job title.
        app.MapGet("/api/occupations/search", async (SearchOccupationsHandler handler, string? q, int? max, CancellationToken cancellationToken) =>
            (await handler.HandleAsync(new SearchOccupationsQuery(q, max ?? 10), cancellationToken)).ToHttpResult());

        return app;
    }
}
