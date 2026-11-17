namespace Jobpilot.Mcp.Tools;

/// <param name="Name">The exact skill name to pass to add_skill.</param>
/// <param name="Synonyms">Other ways ads write the same skill.</param>
public sealed record SkillMatch(string Name, IReadOnlyList<string> Synonyms);
