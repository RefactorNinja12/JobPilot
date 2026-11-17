using System.Reflection;
using NetArchTest.Rules;
using TestResult = NetArchTest.Rules.TestResult;

namespace Jobpilot.Architecture.Tests;

/// <summary>
/// Enforces the Clean Architecture rule: dependencies only point inwards
/// (Api/Mcp/Worker -> Infrastructure -> Application -> Domain).
/// </summary>
public class LayerDependencyTests
{
    private const string ApplicationNamespace = "Jobpilot.Application";
    private const string InfrastructureNamespace = "Jobpilot.Infrastructure";
    private const string ApiNamespace = "Jobpilot.Api";
    private const string McpNamespace = "Jobpilot.Mcp";
    private const string WorkerNamespace = "Jobpilot.Worker";

    private static readonly Assembly _domainAssembly = Domain.AssemblyReference.Assembly;
    private static readonly Assembly _applicationAssembly = Application.AssemblyReference.Assembly;
    private static readonly Assembly _infrastructureAssembly = Infrastructure.AssemblyReference.Assembly;

    [Fact]
    public void Domain_ShouldNotDependOnAnyOtherLayer()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                ApplicationNamespace,
                InfrastructureNamespace,
                ApiNamespace,
                McpNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Application_ShouldOnlyDependOnDomain()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                InfrastructureNamespace,
                ApiNamespace,
                McpNamespace,
                WorkerNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Domain_ShouldNotDependOnEntityFramework()
    {
        var result = Types.InAssembly(_domainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Application_ShouldNotDependOnEntityFramework()
    {
        var result = Types.InAssembly(_applicationAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Fact]
    public void Infrastructure_ShouldNotDependOnEntryPoints()
    {
        var result = Types.InAssembly(_infrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApiNamespace, McpNamespace, WorkerNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(FailingTypes(result));
    }

    [Theory]
    [InlineData("Jobpilot.Domain", new string[0])]
    [InlineData("Jobpilot.Application", new[] { "Jobpilot.Domain" })]
    [InlineData("Jobpilot.Infrastructure", new[] { "Jobpilot.Application" })]
    public void Project_ShouldOnlyReferenceAllowedProjects(string project, string[] allowed) =>
        ProjectFile.ReferencedProjects(project).ShouldBe(allowed, ignoreOrder: true);

    private static string FailingTypes(TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
