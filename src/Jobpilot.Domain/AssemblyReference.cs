using System.Reflection;

namespace Jobpilot.Domain;

/// <summary>Exposes this assembly so it can be located (e.g. in architecture tests and DI scanning).</summary>
public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
