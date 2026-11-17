using System.ComponentModel;
using Jobpilot.Application.Profiles.GetProfile;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>Reading the profile as a whole. Changing skills lives in <see cref="SkillTools"/>.</summary>
[McpServerToolType]
public static class ProfileTools
{
    [McpServerTool(Name = "get_profile", ReadOnly = true, Idempotent = true)]
    [Description(
        "Returns the user's profile: skills (name, level, years of experience) and dealbreakers " +
        "(full-time/part-time, staffing agencies, max commute distance). The profile is the only source of " +
        "facts about the user; never invent skills or experience that are not in it.")]
    public static Task<ProfileView> GetProfile(GetProfileHandler handler, CancellationToken cancellationToken = default) =>
        handler.HandleAsync(cancellationToken);
}
