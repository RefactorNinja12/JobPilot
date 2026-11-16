namespace Jobpilot.Domain.Common;

/// <summary>An expected failure, e.g. invalid input or a missing entity. Not used for bugs.</summary>
/// <param name="Code">Stable machine-readable code, e.g. "Skill.NameEmpty".</param>
/// <param name="Message">Human-readable description.</param>
/// <param name="Type">Failure category.</param>
public sealed record Error(string Code, string Message, ErrorType Type)
{
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}
