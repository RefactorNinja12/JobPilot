namespace Jobpilot.Application.Abstractions.Enrichments;

/// <summary>
/// One Enrichments competence concept, e.g. "Kafka", and the terms that Enrichments recognises as it
/// in ad text, e.g. "kafka" and "apache kafka". The name is what profile skills and extracted competencies use.
/// </summary>
public sealed record SkillConcept(string Name, IReadOnlyList<string> Terms);
