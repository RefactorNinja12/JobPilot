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
    private const string DomainNamespace = "Jobpilot.Domain";
    private const string ApplicationNamespace = "Jobpilot.Application";
    private const string InfrastructureNamespace = "Jobpilot.Infrastructure";
    private const string ApiNamespace = "Jobpilot.Api";
    private const string McpNamespace = "Jobpilot.Mcp";
    private const string WorkerNamespace = "Jobpilot.Worker";

    private static readonly Assembly _domainAssembly = typeof(Domain.AssemblyReference).Assembly;
    private static readonly Assembly _applicationAssembly = typeof(Application.AssemblyReference).Assembly;

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

    private static string FailingTypes(TestResult result) =>
        "Violating types: " + string.Join(", ", result.FailingTypeNames ?? []);
}
