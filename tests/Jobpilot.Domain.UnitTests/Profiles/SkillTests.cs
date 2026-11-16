using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class SkillTests
{
    private const string CSharpTaxonomyId = "rQds_YGd_quU";

    [Fact]
    public void Create_WithValidName_Succeeds()
    {
        var result = Skill.Create("C#");

        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe("C#");
        result.Value.TaxonomyId.ShouldBeNull();
    }

    [Fact]
    public void Create_TrimsNameAndTaxonomyId()
    {
        var skill = Skill.Create("  C#  ", $" {CSharpTaxonomyId} ").Value;

        skill.Name.ShouldBe("C#");
        skill.TaxonomyId.ShouldBe(CSharpTaxonomyId);
    }

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

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankTaxonomyId_TreatsItAsFreeText(string taxonomyId) =>
        Skill.Create("C#", taxonomyId).Value.TaxonomyId.ShouldBeNull();

    [Fact]
    public void FreeTextSkills_WithSameNameDifferentCase_AreEqual()
    {
        var a = Skill.Create("C#").Value;
        var b = Skill.Create("c#").Value;

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void FreeTextSkills_WithDifferentNames_AreNotEqual()
    {
        var a = Skill.Create("C#").Value;
        var b = Skill.Create("Java").Value;

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void TaxonomySkills_WithSameIdDifferentNames_AreEqual()
    {
        var a = Skill.Create("C#", CSharpTaxonomyId).Value;
        var b = Skill.Create("Programmering i C#", CSharpTaxonomyId).Value;

        a.ShouldBe(b);
        a.GetHashCode().ShouldBe(b.GetHashCode());
    }

    [Fact]
    public void TaxonomySkills_WithSameNameDifferentIds_AreNotEqual()
    {
        var a = Skill.Create("Java", "id_1").Value;
        var b = Skill.Create("Java", "id_2").Value;

        a.ShouldNotBe(b);
    }

    [Fact]
    public void TaxonomySkill_AndFreeTextSkill_AreNeverEqual()
    {
        var taxonomy = Skill.Create("C#", CSharpTaxonomyId).Value;
        var freeText = Skill.Create("C#").Value;

        taxonomy.ShouldNotBe(freeText);
        freeText.ShouldNotBe(taxonomy);
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
    public void Skills_InHashSet_AreDeduplicated()
    {
        var skills = new HashSet<Skill>
        {
            Skill.Create("C#").Value,
            Skill.Create("c#").Value,
            Skill.Create("SQL", "sql_id").Value,
            Skill.Create("Structured Query Language", "sql_id").Value,
        };

        skills.Count.ShouldBe(2);
    }

    [Fact]
    public void ToString_ReturnsName() =>
        Skill.Create("C#").Value.ToString().ShouldBe("C#");
}
