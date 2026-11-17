using Jobpilot.Domain.Common;
using Jobpilot.Domain.Jobs;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class ProfileTargetOccupationsTests
{
    private static readonly Occupation _backend = Occupation.Create("7wdX_4rv_33z", "Backend-utvecklare").Value;
    private static readonly Occupation _fullstack = Occupation.Create("71Ji_irM_rSJ", "Fullstack-utvecklare").Value;

    private static Occupation Numbered(int i) => Occupation.Create($"id-{i}", $"Yrke {i}").Value;

    [Fact]
    public void NewProfile_HasNoTargetOccupations() =>
        Profile.Create().TargetOccupations.ShouldBeEmpty();

    [Fact]
    public void Set_ReplacesTheList()
    {
        var profile = Profile.Create();
        profile.SetTargetOccupations([_backend]).IsSuccess.ShouldBeTrue();

        profile.SetTargetOccupations([_fullstack]).IsSuccess.ShouldBeTrue();

        profile.TargetOccupations.ShouldBe([_fullstack]);
    }

    [Fact]
    public void Set_DropsDuplicateIds_KeepingTheFirst()
    {
        var profile = Profile.Create();
        var sameIdOtherLabel = Occupation.Create("7wdX_4rv_33z", "Backend developer").Value;

        profile.SetTargetOccupations([_backend, _fullstack, sameIdOtherLabel]);

        profile.TargetOccupations.ShouldBe([_backend, _fullstack]);
    }

    [Fact]
    public void Set_Empty_Clears()
    {
        var profile = Profile.Create();
        profile.SetTargetOccupations([_backend]);

        profile.SetTargetOccupations([]).IsSuccess.ShouldBeTrue();

        profile.TargetOccupations.ShouldBeEmpty();
    }

    [Fact]
    public void Set_AtTheLimit_Succeeds() =>
        Profile.Create()
            .SetTargetOccupations(Enumerable.Range(0, Profile.MaxTargetOccupations).Select(Numbered))
            .IsSuccess.ShouldBeTrue();

    [Fact]
    public void Set_OverTheLimit_FailsAndKeepsTheCurrentList()
    {
        var profile = Profile.Create();
        profile.SetTargetOccupations([_backend]);

        var result = profile.SetTargetOccupations(Enumerable.Range(0, Profile.MaxTargetOccupations + 1).Select(Numbered));

        result.Error.ShouldBe(ProfileErrors.TooManyTargetOccupations);
        result.Error.Type.ShouldBe(ErrorType.Validation);
        profile.TargetOccupations.ShouldBe([_backend]);
    }
}
