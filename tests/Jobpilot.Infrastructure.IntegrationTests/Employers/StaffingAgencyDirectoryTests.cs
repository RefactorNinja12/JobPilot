using Jobpilot.Application.Abstractions.Employers;
using Jobpilot.Domain.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Jobpilot.Infrastructure.IntegrationTests.Employers;

/// <summary>Resolves the staffing agency directory from AddInfrastructure with in-memory configuration.</summary>
public sealed class StaffingAgencyDirectoryTests : IDisposable
{
    private ServiceProvider? _provider;

    public void Dispose() => _provider?.Dispose();

    private IStaffingAgencyDirectory Resolve(params string[] organizationNumbers)
    {
        var settings = organizationNumbers
            .Select((n, i) => KeyValuePair.Create<string, string?>($"StaffingAgencies:OrganizationNumbers:{i}", n));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        _provider = new ServiceCollection().AddInfrastructure(configuration).BuildServiceProvider();
        return _provider.GetRequiredService<IStaffingAgencyDirectory>();
    }

    private static Employer EmployerWith(string? organizationNumber) =>
        Employer.Create("Bemanning AB", organizationNumber).Value;

    [Theory]
    [InlineData("556677-8899")]
    [InlineData(" 5566778899 ")]
    public void ListedNumber_InEitherFormat_IsAStaffingAgency(string configured) =>
        Resolve(configured).IsStaffingAgency(EmployerWith("5566778899")).ShouldBeTrue();

    [Fact]
    public void UnlistedNumber_IsNotAStaffingAgency() =>
        Resolve("556677-8899").IsStaffingAgency(EmployerWith("2021005448")).ShouldBeFalse();

    [Fact]
    public void EmployerWithoutNumber_IsNotAStaffingAgency() =>
        Resolve("556677-8899").IsStaffingAgency(EmployerWith(null)).ShouldBeFalse();

    [Fact]
    public void EmptyList_MatchesNobody() =>
        Resolve().IsStaffingAgency(EmployerWith("5566778899")).ShouldBeFalse();

    [Theory]
    [InlineData("556677-889")]
    [InlineData("Manpower")]
    public void InvalidNumber_FailsOnResolve(string configured)
    {
        var ex = Should.Throw<OptionsValidationException>(() => Resolve("556677-8899", configured));

        ex.Message.ShouldContain("StaffingAgencies:OrganizationNumbers");
    }
}
