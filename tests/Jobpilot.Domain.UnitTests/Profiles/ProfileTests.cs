using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class ProfileTests
{
    private static readonly Skill _csharp = Skill.Create("C#", "rQds_YGd_quU").Value;
    private static readonly Skill _sql = Skill.Create("SQL").Value;

    private static ProfileSkill CSharp(int years = 4) =>
        ProfileSkill.Create(_csharp, SkillLevel.Advanced, years).Value;

    [Fact]
    public void Create_GivesUniqueIdAndNoSkills()
    {
        var a = Profile.Create();
        var b = Profile.Create();

        a.Id.ShouldNotBe(Guid.Empty);
        a.Id.ShouldNotBe(b.Id);
        a.Skills.ShouldBeEmpty();
    }

    [Fact]
    public void AddSkill_NewSkill_IsAdded()
    {
        var profile = Profile.Create();

        var result = profile.AddSkill(CSharp());

        result.IsSuccess.ShouldBeTrue();
        profile.Skills.ShouldHaveSingleItem().ShouldBe(CSharp());
    }

    [Fact]
    public void AddSkill_SameSkillTwice_FailsWithConflict()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp(years: 4));

        var result = profile.AddSkill(CSharp(years: 2));

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Profile.SkillAlreadyAdded");
        profile.Skills.ShouldHaveSingleItem().YearsOfExperience.ShouldBe(4);
    }

    [Fact]
    public void AddSkill_SameTaxonomyIdDifferentName_CountsAsDuplicate()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp());
        var renamed = Skill.Create("Programmering i C#", "rQds_YGd_quU").Value;

        var result = profile.AddSkill(ProfileSkill.Create(renamed, SkillLevel.Beginner, 1).Value);

        result.Error.Code.ShouldBe("Profile.SkillAlreadyAdded");
    }

    [Fact]
    public void AddSkill_DifferentSkills_AreAllKept()
    {
        var profile = Profile.Create();

        profile.AddSkill(CSharp());
        profile.AddSkill(ProfileSkill.Create(_sql, SkillLevel.Intermediate, 2).Value);

        profile.Skills.Count.ShouldBe(2);
    }

    [Fact]
    public void UpdateSkill_ExistingSkill_ReplacesLevelAndYears()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp(years: 4));

        var result = profile.UpdateSkill(ProfileSkill.Create(_csharp, SkillLevel.Expert, 6).Value);

        result.IsSuccess.ShouldBeTrue();
        var updated = profile.Skills.ShouldHaveSingleItem();
        updated.Level.ShouldBe(SkillLevel.Expert);
        updated.YearsOfExperience.ShouldBe(6);
    }

    [Fact]
    public void UpdateSkill_MissingSkill_FailsWithNotFound()
    {
        var profile = Profile.Create();

        var result = profile.UpdateSkill(CSharp());

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        profile.Skills.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveSkill_ExistingSkill_IsRemoved()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp());

        var result = profile.RemoveSkill(_csharp);

        result.IsSuccess.ShouldBeTrue();
        profile.Skills.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveSkill_MissingSkill_FailsWithNotFound()
    {
        var profile = Profile.Create();

        var result = profile.RemoveSkill(_sql);

        result.Error.Type.ShouldBe(ErrorType.NotFound);
        result.Error.Code.ShouldBe("Profile.SkillNotFound");
    }

    [Fact]
    public void NullArguments_AreBugsAndThrow()
    {
        var profile = Profile.Create();

        Should.Throw<ArgumentNullException>(() => profile.AddSkill(null!));
        Should.Throw<ArgumentNullException>(() => profile.UpdateSkill(null!));
        Should.Throw<ArgumentNullException>(() => profile.RemoveSkill(null!));
    }

    [Fact]
    public void Skills_CannotBeModifiedFromOutside() =>
        Profile.Create().Skills.ShouldBeAssignableTo<System.Collections.ObjectModel.ReadOnlyCollection<ProfileSkill>>();

    [Fact]
    public void Create_StartsWithNoDealbreakers() =>
        Profile.Create().Preferences.ShouldBe(JobPreferences.None);

    [Fact]
    public void UpdatePreferences_ReplacesPreferences()
    {
        var profile = Profile.Create();
        var preferences = JobPreferences.Create(true, false, true).Value;

        profile.UpdatePreferences(preferences);

        profile.Preferences.ShouldBe(preferences);
    }

    [Fact]
    public void UpdatePreferences_WithNull_Throws() =>
        Should.Throw<ArgumentNullException>(() => Profile.Create().UpdatePreferences(null!));
}
