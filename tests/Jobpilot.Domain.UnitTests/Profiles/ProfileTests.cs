using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class ProfileTests
{
    private static readonly Skill _csharp = Skill.Create("C#").Value;
    private static readonly Skill _sql = Skill.Create("SQL").Value;

    private static ProfileSkill CSharp(SkillLevel level = SkillLevel.Advanced) =>
        ProfileSkill.Create(_csharp, level).Value;

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
        profile.AddSkill(CSharp(SkillLevel.Advanced));

        var result = profile.AddSkill(CSharp(SkillLevel.Beginner));

        result.Error.Type.ShouldBe(ErrorType.Conflict);
        result.Error.Code.ShouldBe("Profile.SkillAlreadyAdded");
        profile.Skills.ShouldHaveSingleItem().Level.ShouldBe(SkillLevel.Advanced);
    }

    [Fact]
    public void AddSkill_SameNameDifferentCase_CountsAsDuplicate()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp());
        var lowerCase = Skill.Create("c#").Value;

        var result = profile.AddSkill(ProfileSkill.Create(lowerCase, SkillLevel.Beginner).Value);

        result.Error.Code.ShouldBe("Profile.SkillAlreadyAdded");
    }

    [Fact]
    public void AddSkill_DifferentSkills_AreAllKept()
    {
        var profile = Profile.Create();

        profile.AddSkill(CSharp());
        profile.AddSkill(ProfileSkill.Create(_sql, SkillLevel.Intermediate).Value);

        profile.Skills.Count.ShouldBe(2);
    }

    [Fact]
    public void UpdateSkill_ExistingSkill_ReplacesLevel()
    {
        var profile = Profile.Create();
        profile.AddSkill(CSharp());

        var result = profile.UpdateSkill(ProfileSkill.Create(_csharp, SkillLevel.Expert).Value);

        result.IsSuccess.ShouldBeTrue();
        var updated = profile.Skills.ShouldHaveSingleItem();
        updated.Level.ShouldBe(SkillLevel.Expert);
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
        var preferences = JobPreferences.Create(acceptsFullTime: true, acceptsPartTime: false, excludeStaffingAgencies: true).Value;

        profile.UpdatePreferences(preferences);

        profile.Preferences.ShouldBe(preferences);
    }

    [Fact]
    public void UpdatePreferences_WithNull_Throws() =>
        Should.Throw<ArgumentNullException>(() => Profile.Create().UpdatePreferences(null!));
}
