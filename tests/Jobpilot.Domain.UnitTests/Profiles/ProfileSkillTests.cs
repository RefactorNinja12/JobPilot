using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class ProfileSkillTests
{
    private static readonly Skill _csharp = Skill.Create("C#").Value;

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = ProfileSkill.Create(_csharp, SkillLevel.Advanced, 4);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Skill.ShouldBe(_csharp);
        result.Value.Level.ShouldBe(SkillLevel.Advanced);
        result.Value.YearsOfExperience.ShouldBe(4);
    }

    [Fact]
    public void Create_WithoutSkill_FailsWithSkillMissing() =>
        ProfileSkill.Create(skill: null, SkillLevel.Beginner, 0).Error.ShouldBe(ProfileSkillErrors.SkillMissing);

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Create_WithUndefinedLevel_FailsWithLevelInvalid(int level) =>
        ProfileSkill.Create(_csharp, (SkillLevel)level, 1).Error.ShouldBe(ProfileSkillErrors.LevelInvalid);

    [Theory]
    [InlineData(0)]
    [InlineData(ProfileSkill.MaxYearsOfExperience)]
    public void Create_WithYearsAtBoundary_Succeeds(int years) =>
        ProfileSkill.Create(_csharp, SkillLevel.Intermediate, years).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(-1)]
    [InlineData(ProfileSkill.MaxYearsOfExperience + 1)]
    public void Create_WithYearsOutOfRange_FailsWithYearsOutOfRange(int years) =>
        ProfileSkill.Create(_csharp, SkillLevel.Intermediate, years).Error.ShouldBe(ProfileSkillErrors.YearsOutOfRange);

    [Fact]
    public void Create_ExpertWithFewYears_IsAllowed() =>
        ProfileSkill.Create(_csharp, SkillLevel.Expert, 1).IsSuccess.ShouldBeTrue();

    [Fact]
    public void ProfileSkills_WithSameValues_AreEqual()
    {
        var a = ProfileSkill.Create(_csharp, SkillLevel.Advanced, 4).Value;
        var b = ProfileSkill.Create(Skill.Create("c#").Value, SkillLevel.Advanced, 4).Value;

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void ProfileSkills_WithDifferentYears_AreNotEqual()
    {
        var a = ProfileSkill.Create(_csharp, SkillLevel.Advanced, 4).Value;
        var b = ProfileSkill.Create(_csharp, SkillLevel.Advanced, 5).Value;

        a.ShouldNotBe(b);
    }
}
