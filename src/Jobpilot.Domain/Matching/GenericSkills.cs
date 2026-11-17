using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.Matching;

/// <summary>
/// Broad terms Enrichments finds in almost every developer ad but nobody lists as a skill ("Systemutveckling",
/// "Teknik", "Design"). Counting them as missing would lower every fit score for no reason, and a prediction
/// threshold cannot separate them: they score 0.87-0.96 like real skills. Chosen from the most frequent concepts in
/// 200 real ads (2026-11-17), conservatively: languages, education and clearances stay, as they are real requirements.
/// </summary>
public static class GenericSkills
{
    private static readonly HashSet<string> _names = new(StringComparer.OrdinalIgnoreCase)
    {
        "Analys",
        "Arbetslivserfarenhet",
        "Automation",
        "Automatisering",
        "Backend",
        "Data",
        "Datateknik",
        "Design",
        "Dokumentation",
        "Felsökning",
        "Frontend",
        "Förvaltning",
        "Implementering",
        "Infrastruktur",
        "Integration",
        "It-intresse",
        "Kommunikation",
        "Konfiguration",
        "Konfigurering",
        "Kultur",
        "Mjukvara",
        "Mjukvaruutveckling",
        "Organisationer",
        "Problemlösning",
        "Programmering",
        "Research",
        "Systemarbete",
        "Systemutveckling",
        "Systemvetenskap",
        "Teknik",
        "Tekniska lösningar",
        "Utvecklingsarbete",
        "Utvecklingsmiljö",
        "Utvecklingsteam",
        "Utvecklingsverktyg",
        "Verktyg",
        "Vidareutveckling",
    };

    /// <summary>True when the skill is one of the broad terms that fit scoring ignores.</summary>
    public static bool Contains(Skill skill)
    {
        ArgumentNullException.ThrowIfNull(skill);
        return _names.Contains(skill.Name);
    }
}
