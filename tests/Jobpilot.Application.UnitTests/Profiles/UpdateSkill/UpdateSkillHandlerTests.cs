using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.GetProfile;
using Jobpilot.Application.Profiles.UpdateSkill;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.UpdateSkill;

public class UpdateSkillHandlerTests
{
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ISkillVocabularyStore _vocabularies = Substitute.For<ISkillVocabularyStore>();
    private readonly Profile _profile = Profile.Create();
    private readonly UpdateSkillHandler _handler;

    public UpdateSkillHandlerTests()
    {
        _profile.AddSkill(ProfileSkill.Create(Skill.Create("Kafka").Value, SkillLevel.Beginner, 1).Value);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _vocabularies.GetAsync(Arg.Any<CancellationToken>()).Returns(SkillVocabulary.Create(
            DateTimeOffset.UnixEpoch,
            [new SkillConcept("Kafka", ["kafka", "apache kafka"])]));
        _handler = new UpdateSkillHandler(_profiles, _vocabularies);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private Task<Result<ProfileSkillView>> UpdateAsync(string name, SkillLevel level = SkillLevel.Expert, int years = 4) =>
        _handler.HandleAsync(new UpdateSkillCommand(name, level, years), Ct);

    [Theory]
    [InlineData("Kafka")]
    [InlineData("apache kafka")]
    [InlineData(" KAFKA ")]
    public async Task Update_ByNameOrSynonym_ReplacesLevelAndYears(string name)
    {
        var result = await UpdateAsync(name);

        result.Value.ShouldBe(new ProfileSkillView("Kafka", "Expert", 4));
        var skill = _profile.Skills.ShouldHaveSingleItem();
        skill.Level.ShouldBe(SkillLevel.Expert);
        skill.YearsOfExperience.ShouldBe(4);
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Update_SkillNotInProfile_FailsAsNotFound()
    {
        var error = (await UpdateAsync("C#")).Error;

        error.Code.ShouldBe("Profile.SkillNotFound");
        error.Type.ShouldBe(ErrorType.NotFound);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_WithoutProfile_FailsAsNotFound()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);

        (await UpdateAsync("Kafka")).Error.Code.ShouldBe("Profile.SkillNotFound");
    }

    [Theory]
    [InlineData("", 3, "Skill.NameEmpty")]
    [InlineData("Kafka", 51, "ProfileSkill.YearsOutOfRange")]
    public async Task Update_InvalidInput_FailsWithoutChangingTheProfile(string name, int years, string code)
    {
        (await UpdateAsync(name, years: years)).Error.Code.ShouldBe(code);

        _profile.Skills.ShouldHaveSingleItem().Level.ShouldBe(SkillLevel.Beginner);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
