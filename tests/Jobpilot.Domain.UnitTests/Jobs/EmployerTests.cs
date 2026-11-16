using Jobpilot.Domain.Jobs;

namespace Jobpilot.Domain.UnitTests.Jobs;

public class EmployerTests
{
    [Fact]
    public void Create_WithNameAndOrganizationNumber_Succeeds()
    {
        var result = Employer.Create("Skatteverket", "2021005448");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("Skatteverket");
        result.Value.OrganizationNumber.ShouldBe("2021005448");
    }

    [Fact]
    public void Create_TrimsNameAndRemovesHyphenFromOrganizationNumber()
    {
        var employer = Employer.Create("  Skatteverket  ", " 202100-5448 ").Value;

        employer.Name.ShouldBe("Skatteverket");
        employer.OrganizationNumber.ShouldBe("2021005448");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutOrganizationNumber_SucceedsWithNull(string? organizationNumber) =>
        Employer.Create("Skatteverket", organizationNumber).Value.OrganizationNumber.ShouldBeNull();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutName_FailsWithEmployerNameEmpty(string? name) =>
        Employer.Create(name, "2021005448").Error.ShouldBe(JobAdErrors.EmployerNameEmpty);

    [Theory]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    [InlineData("20210O5448")]
    [InlineData("2021-00-5448-1")]
    public void Create_WithMalformedOrganizationNumber_FailsWithOrganizationNumberInvalid(string organizationNumber) =>
        Employer.Create("Skatteverket", organizationNumber).Error.ShouldBe(JobAdErrors.OrganizationNumberInvalid);
}
