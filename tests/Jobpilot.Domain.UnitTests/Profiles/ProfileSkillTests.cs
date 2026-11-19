using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class ProfileSkillTests
{
    private static readonly Skill _csharp = Skill.Create("C#").Value;

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = ProfileSkill.Create(_csharp, SkillLevel.Advanced);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Skill.ShouldBe(_csharp);
        result.Value.Level.ShouldBe(SkillLevel.Advanced);
    }

    [Fact]
    public void Create_WithoutSkill_FailsWithSkillMissing() =>
        ProfileSkill.Create(skill: null, SkillLevel.Beginner).Error.ShouldBe(ProfileSkillErrors.SkillMissing);

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    public void Create_WithUndefinedLevel_FailsWithLevelInvalid(int level) =>
        ProfileSkill.Create(_csharp, (SkillLevel)level).Error.ShouldBe(ProfileSkillErrors.LevelInvalid);

    [Fact]
    public void ProfileSkills_WithSameValues_AreEqual()
    {
        var a = ProfileSkill.Create(_csharp, SkillLevel.Advanced).Value;
        var b = ProfileSkill.Create(Skill.Create("c#").Value, SkillLevel.Advanced).Value;

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void ProfileSkills_WithDifferentLevels_AreNotEqual()
    {
        var a = ProfileSkill.Create(_csharp, SkillLevel.Advanced).Value;
        var b = ProfileSkill.Create(_csharp, SkillLevel.Expert).Value;

        a.ShouldNotBe(b);
    }
}
