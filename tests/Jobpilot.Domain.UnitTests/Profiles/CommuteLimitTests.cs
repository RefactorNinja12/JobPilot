using Jobpilot.Domain.Locations;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Domain.UnitTests.Profiles;

public class CommuteLimitTests
{
    private static readonly Coordinates _stockholm = Coordinates.Create(59.3293, 18.0686).Value;
    private static readonly Coordinates _uppsala = Coordinates.Create(59.8586, 17.6389).Value; // ~64 km
    private static readonly Coordinates _goteborg = Coordinates.Create(57.7089, 11.9746).Value; // ~398 km

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var result = CommuteLimit.Create(_stockholm, 50);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Home.ShouldBe(_stockholm);
        result.Value.MaxDistanceKm.ShouldBe(50);
    }

    [Fact]
    public void Create_WithoutHome_FailsWithHomeMissing() =>
        CommuteLimit.Create(null, 50).Error.ShouldBe(CommuteLimitErrors.HomeMissing);

    [Theory]
    [InlineData(1)]
    [InlineData(CommuteLimit.MaxDistanceLimitKm)]
    public void Create_WithDistanceAtBoundary_Succeeds(int km) =>
        CommuteLimit.Create(_stockholm, km).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(CommuteLimit.MaxDistanceLimitKm + 1)]
    public void Create_WithDistanceOutOfRange_FailsWithDistanceOutOfRange(int km) =>
        CommuteLimit.Create(_stockholm, km).Error.ShouldBe(CommuteLimitErrors.DistanceOutOfRange);

    [Fact]
    public void IsWithinReach_WorkplaceCloserThanLimit_IsTrue() =>
        CommuteLimit.Create(_stockholm, 100).Value.IsWithinReach(_uppsala).ShouldBeTrue();

    [Fact]
    public void IsWithinReach_WorkplaceFartherThanLimit_IsFalse() =>
        CommuteLimit.Create(_stockholm, 50).Value.IsWithinReach(_uppsala).ShouldBeFalse();

    [Fact]
    public void IsWithinReach_WorkplaceAtHome_IsTrue() =>
        CommuteLimit.Create(_stockholm, 1).Value.IsWithinReach(_stockholm).ShouldBeTrue();

    [Fact]
    public void IsWithinReach_WorkplaceExactlyAtLimit_IsTrue()
    {
        var limitKm = (int)Math.Ceiling(_stockholm.DistanceInKmTo(_goteborg));

        CommuteLimit.Create(_stockholm, limitKm).Value.IsWithinReach(_goteborg).ShouldBeTrue();
        CommuteLimit.Create(_stockholm, limitKm - 1).Value.IsWithinReach(_goteborg).ShouldBeFalse();
    }

    [Fact]
    public void IsWithinReach_Null_Throws() =>
        Should.Throw<ArgumentNullException>(() => CommuteLimit.Create(_stockholm, 50).Value.IsWithinReach(null!));
}
