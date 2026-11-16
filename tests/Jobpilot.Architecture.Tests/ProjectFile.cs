using System.Xml.Linq;

namespace Jobpilot.Architecture.Tests;

/// <summary>
/// Reads &lt;ProjectReference&gt; entries straight from .csproj files. Unlike type-based checks,
/// this catches a forbidden project reference even before any type from it is used
/// (the compiler drops unused references from the built assembly).
/// </summary>
internal static class ProjectFile
{
    private const string SolutionFileName = "Jobpilot.slnx";

    public static string[] ReferencedProjects(string projectName)
    {
        var path = Path.Combine(FindRepositoryRoot(), "src", projectName, projectName + ".csproj");

        return [.. XDocument.Load(path)
            .Descendants("ProjectReference")
            .Select(reference => Path.GetFileNameWithoutExtension(
                reference.Attribute("Include")!.Value.Replace('\\', Path.DirectorySeparatorChar)))];
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException($"Could not find {SolutionFileName} above {AppContext.BaseDirectory}.");
    }
}
