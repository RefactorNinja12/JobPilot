using Jobpilot.Domain.Matching;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Matching;

public class GenericSkillsTests
{
    [Theory]
    [InlineData("Systemutveckling")]
    [InlineData("TEKNIK")]
    [InlineData("tekniska lösningar")]
    public void BroadTerms_AreGeneric_IgnoringCase(string name) =>
        GenericSkills.Contains(Skill.Create(name).Value).ShouldBeTrue();

    [Theory]
    [InlineData("C#")]
    [InlineData("Kafka")]
    [InlineData("Svenska")]
    [InlineData("Universitetsutbildning")]
    public void ConcreteSkillsAndRequirements_AreNotGeneric(string name) =>
        GenericSkills.Contains(Skill.Create(name).Value).ShouldBeFalse();
}
