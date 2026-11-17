using System.ComponentModel;
using Jobpilot.Application.Profiles.AddSkill;
using Jobpilot.Application.SkillVocabularies.Search;
using Jobpilot.Domain.Profiles;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Jobpilot.Mcp.Tools;

/// <summary>Skill tools: find the exact skill name, then add it to the profile.</summary>
[McpServerToolType]
public static class SkillTools
{
    [McpServerTool(Name = "search_skills", ReadOnly = true, Idempotent = true)]
    [Description(
        "Searches the skill vocabulary that job ads are analysed with (JobTech Enrichments). " +
        "Matches skill names and synonyms, best first. Use it to find the exact name before add_skill.")]
    public static async Task<IReadOnlyList<SkillMatch>> SearchSkills(
        SearchSkillsHandler handler,
        [Description("Text to search for, e.g. \"kafka\" or \"c sharp\".")] string text,
        [Description("Maximum number of results, 1-50.")] int maxResults = 10,
        CancellationToken cancellationToken = default)
    {
        var concepts = (await handler.HandleAsync(new SearchSkillsQuery(text, maxResults), cancellationToken))
            .ValueOrToolError();
        return [.. concepts.Select(c => new SkillMatch(c.Name, c.Terms))];
    }

    [McpServerTool(Name = "add_skill", Destructive = false, Idempotent = false)]
    [Description(
        "Adds a skill to the user's profile. A name or synonym from the vocabulary is stored as its exact skill name. " +
        "If the result has inVocabulary = false, the skill will never match a job ad: tell the user and offer " +
        "search_skills suggestions.")]
    public static async Task<AddedSkill> AddSkill(
        AddSkillHandler handler,
        [Description("Skill name or synonym, e.g. \"Kafka\" or \"apache kafka\".")] string name,
        [Description("Self-assessed level: Beginner, Intermediate, Advanced or Expert.")] string level,
        [Description("Whole years of hands-on experience, 0-50.")] int yearsOfExperience,
        CancellationToken cancellationToken = default)
    {
        return (await handler.HandleAsync(new AddSkillCommand(name, ParseLevel(level), yearsOfExperience), cancellationToken))
            .ValueOrToolError();
    }

    // Names only: Enum.TryParse would also accept numbers such as "3".
    private static SkillLevel ParseLevel(string? level) =>
        Enum.GetValues<SkillLevel>()
            .Select(l => (SkillLevel?)l)
            .FirstOrDefault(l => string.Equals(l.ToString(), level?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new McpException($"Unknown level '{level}'. Use Beginner, Intermediate, Advanced or Expert.");
}
