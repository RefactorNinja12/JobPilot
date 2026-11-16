using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class SkillTests
{
    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        var result = Skill.Create("C#");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("C#");
    }

    [Fact]
    public void Create_TrimsName() =>
        Skill.Create("  C#  ").Value.Name.ShouldBe("C#");

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingName_FailsWithNameEmpty(string? name) =>
        Skill.Create(name).Error.ShouldBe(SkillErrors.NameEmpty);

    [Fact]
    public void Create_WithNameAtMaxLength_Succeeds() =>
        Skill.Create(new string('a', Skill.MaxNameLength)).IsSuccess.ShouldBeTrue();

    [Fact]
    public void Create_WithNameOverMaxLength_FailsWithNameTooLong() =>
        Skill.Create(new string('a', Skill.MaxNameLength + 1)).Error.ShouldBe(SkillErrors.NameTooLong);

    [Fact]
    public void Skills_WithSameNameDifferentCase_AreEqual()
    {
        var a = Skill.Create("Postgresql").Value;
        var b = Skill.Create("PostgreSQL").Value;

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void Skills_WithDifferentNames_AreNotEqual()
    {
        var a = Skill.Create("C#").Value;
        var b = Skill.Create("Java").Value;

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_WithNull_IsFalse()
    {
        var skill = Skill.Create("C#").Value;

        skill.Equals(null).ShouldBeFalse();
        (skill == null).ShouldBeFalse();
        (null == skill).ShouldBeFalse();
    }

    [Fact]
    public void Equality_WithOtherType_IsFalse() =>
        Skill.Create("C#").Value.Equals("C#").ShouldBeFalse();

    [Fact]
    public void Skills_InHashSet_AreDeduplicated()
    {
        var skills = new HashSet<Skill>
        {
            Skill.Create("C#").Value,
            Skill.Create("c#").Value,
            Skill.Create(".net").Value,
            Skill.Create(".NET").Value,
        };

        skills.Count.ShouldBe(2);
    }

    [Fact]
    public void ToString_ReturnsName() =>
        Skill.Create("C#").Value.ToString().ShouldBe("C#");
}
