using Jobpilot.Application.Profiles.GetProfile;

namespace Jobpilot.Api.Endpoints;

internal static class ProfileEndpoints
{
    public static IEndpointRouteBuilder MapProfileEndpoints(this IEndpointRouteBuilder app)
    {
        var profile = app.MapGroup("/api/profile");

        // Skills, target occupations and dealbreakers; an empty profile before anything is added.
        profile.MapGet("/", async (GetProfileHandler handler, CancellationToken cancellationToken) =>
            Results.Ok(await handler.HandleAsync(cancellationToken)));

        return app;
    }
}
