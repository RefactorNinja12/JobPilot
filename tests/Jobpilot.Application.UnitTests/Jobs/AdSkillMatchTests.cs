using Jobpilot.Application.Abstractions.Enrichments;
using Jobpilot.Application.Jobs;
using Jobpilot.Domain.Profiles;

namespace Jobpilot.Application.UnitTests.Jobs;

public class AdSkillMatchTests
{
    private static Profile ProfileWith(params (string Name, SkillLevel Level)[] skills)
    {
        var profile = Profile.Create();
        foreach (var (name, level) in skills)
        {
            profile.AddSkill(ProfileSkill.Create(Skill.Create(name).Value, level).Value);
        }

        return profile;
    }

    [Fact]
    public void Calculate_SplitsTheAdsSkillsIntoMatchedAndMissing()
    {
        var profile = ProfileWith(("C#", SkillLevel.Expert), ("SQL", SkillLevel.Advanced));

        var score = AdSkillMatch.Calculate(profile, [new("C#", 0.9), new("Kafka", 0.8), new("sql", 0.5)]);

        score.Matched.ShouldBe(["C#", "SQL"]);
        score.Missing.ShouldBe(["Kafka"]);
    }

    [Fact]
    public void Calculate_IgnoresLabelsThatCannotBeSkills()
    {
        var profile = ProfileWith(("C#", SkillLevel.Expert));

        var score = AdSkillMatch.Calculate(profile, [new("C#", 0.9), new(new string('x', Skill.MaxNameLength + 1), 0.9)]);

        score.Matched.ShouldBe(["C#"]);
        score.Missing.ShouldBeEmpty();
    }

    [Fact]
    public void Calculate_AdWithoutSkills_HasNothingToShow()
    {
        var score = AdSkillMatch.Calculate(ProfileWith(("C#", SkillLevel.Expert)), []);

        score.Value.ShouldBe(0);
        score.Matched.ShouldBeEmpty();
        score.Missing.ShouldBeEmpty();
    }
}
