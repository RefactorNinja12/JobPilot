using Jobpilot.Domain.Jobs;

namespace Jobpilot.Domain.UnitTests.Jobs;

public class OccupationTests
{
    [Fact]
    public void Create_WithIdAndLabel_SucceedsTrimmed()
    {
        var occupation = Occupation.Create(" fg7B_yov_smw ", " Systemutvecklare/Programmerare ").Value;

        occupation.TaxonomyId.ShouldBe("fg7B_yov_smw");
        occupation.Label.ShouldBe("Systemutvecklare/Programmerare");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithoutId_FailsWithOccupationIdEmpty(string? id) =>
        Occupation.Create(id, "Systemutvecklare").Error.ShouldBe(JobAdErrors.OccupationIdEmpty);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Create_WithoutLabel_FailsWithOccupationLabelEmpty(string? label) =>
        Occupation.Create("fg7B_yov_smw", label).Error.ShouldBe(JobAdErrors.OccupationLabelEmpty);
}
