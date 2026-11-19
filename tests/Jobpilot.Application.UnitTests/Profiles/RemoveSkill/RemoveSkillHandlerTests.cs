using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Profiles;
using Jobpilot.Application.Profiles.RemoveSkill;
using Jobpilot.Domain.Common;
using Jobpilot.Domain.Profiles;
using NSubstitute;

namespace Jobpilot.Application.UnitTests.Profiles.RemoveSkill;

public class RemoveSkillHandlerTests
{
    private readonly IProfileRepository _profiles = Substitute.For<IProfileRepository>();
    private readonly ISkillVocabularyStore _vocabularies = Substitute.For<ISkillVocabularyStore>();
    private readonly Profile _profile = Profile.Create();
    private readonly RemoveSkillHandler _handler;

    public RemoveSkillHandlerTests()
    {
        _profile.AddSkill(ProfileSkill.Create(Skill.Create("Kafka").Value, SkillLevel.Advanced).Value);
        _profile.AddSkill(ProfileSkill.Create(Skill.Create("C#").Value, SkillLevel.Expert).Value);
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns(_profile);
        _vocabularies.GetAsync(Arg.Any<CancellationToken>()).Returns(SkillVocabulary.Create(
            DateTimeOffset.UnixEpoch,
            [new SkillConcept("Kafka", ["kafka", "apache kafka"])]));
        _handler = new RemoveSkillHandler(_profiles, _vocabularies);
    }

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Theory]
    [InlineData("Kafka")]
    [InlineData("apache kafka")]
    public async Task Remove_ByNameOrSynonym_RemovesOnlyThatSkill(string name)
    {
        var result = await _handler.HandleAsync(name, Ct);

        result.Value.ShouldBe("Kafka");
        _profile.Skills.ShouldHaveSingleItem().Skill.Name.ShouldBe("C#");
        await _profiles.Received(1).SaveChangesAsync(Ct);
    }

    [Fact]
    public async Task Remove_SkillNotInProfile_FailsAsNotFound()
    {
        var error = (await _handler.HandleAsync("Rust", Ct)).Error;

        error.Code.ShouldBe("Profile.SkillNotFound");
        error.Type.ShouldBe(ErrorType.NotFound);
        _profile.Skills.Count.ShouldBe(2);
        await _profiles.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_WithoutProfile_FailsAsNotFound()
    {
        _profiles.GetAsync(Arg.Any<CancellationToken>()).Returns((Profile?)null);

        (await _handler.HandleAsync("Kafka", Ct)).Error.Code.ShouldBe("Profile.SkillNotFound");
    }

    [Fact]
    public async Task Remove_BlankName_FailsAsValidation() =>
        (await _handler.HandleAsync(" ", Ct)).Error.Code.ShouldBe("Skill.NameEmpty");
}
