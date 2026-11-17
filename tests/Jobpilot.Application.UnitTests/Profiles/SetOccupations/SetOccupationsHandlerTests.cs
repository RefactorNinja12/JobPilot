using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.SetOccupations;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.SetOccupations;

public class SetOccupationsHandlerTests
{
    private static readonly OccupationView _backend = new("7wdX_4rv_33z", "Backend-utvecklare");
    private static readonly OccupationView _fullstack = new("71Ji_irM_rSJ", "Fullstack-utvecklare");

    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly Profile _profile = Profile.Create();
    private readonly SetOccupationsHandler _handler;

    public SetOccupationsHandlerTests()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _handler = new SetOccupationsHandler(_profiles);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Set_StoresAndReturnsTheOccupations()
    {
        var result = await _handler.HandleAsync([_backend, _fullstack, _backend], Ct);

        result.Value.ShouldBe([_backend, _fullstack]);
        _profile.TargetOccupations.Select(OccupationView.From).ShouldBe([_backend, _fullstack]);
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Set_Empty_ClearsThem()
    {
        await _handler.HandleAsync([_backend], Ct);

        (await _handler.HandleAsync([], Ct)).Value.ShouldBeEmpty();

        _profile.TargetOccupations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Set_WithoutProfile_CreatesOne()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);
        Profile? created = null;
        _profiles.Add(Arg.Do<Profile>(p => created = p));

        await _handler.HandleAsync([_backend], Ct);

        created.ShouldNotBeNull().TargetOccupations.ShouldHaveSingleItem().TaxonomyId.ShouldBe("7wdX_4rv_33z");
    }

    [Theory]
    [InlineData("", "Backend-utvecklare", "Occupation.IdEmpty")]
    [InlineData("7wdX_4rv_33z", " ", "Occupation.LabelEmpty")]
    public async Task Set_InvalidOccupation_FailsWithoutSaving(string id, string label, string code)
    {
        (await _handler.HandleAsync([_fullstack, new OccupationView(id, label)], Ct)).Error.Code.ShouldBe(code);

        _profile.TargetOccupations.ShouldBeEmpty();
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Set_TooMany_FailsWithoutSaving()
    {
        var eleven = Enumerable.Range(0, Profile.MaxTargetOccupations + 1)
            .Select(i => new OccupationView("id-" + i.ToString(System.Globalization.CultureInfo.InvariantCulture), "Yrke"))
            .ToList();

        (await _handler.HandleAsync(eleven, Ct)).Error.ShouldBe(ProfileErrors.TooManyTargetOccupations);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
