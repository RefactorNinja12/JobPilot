using Jobpilot.Application.Abstractions.JobSearch;

namespace Jobpilot.Application.UnitTests.Abstractions.JobSearch;

public class JobSearchQueryTests
{
    private const string Developer = "fg7B_yov_smw";
    private const string Tester = "test_occ_02"; // any id; the query does not validate against Taxonomy

    [Fact]
    public void Create_WithValidInput_Succeeds()
    {
        var after = new DateTimeOffset(2026, 11, 1, 0, 0, 0, TimeSpan.FromHours(1));

        var query = JobSearchQuery.Create([Developer, Tester], 200, after).Value;

        query.OccupationIds.ShouldBe([Developer, Tester]);
        query.MaxResults.ShouldBe(200);
        query.PublishedAfter.ShouldBe(after);
    }

    [Fact]
    public void Create_WithoutPublishedAfter_HasNoLowerBound() =>
        JobSearchQuery.Create([Developer], 10).Value.PublishedAfter.ShouldBeNull();

    [Fact]
    public void Create_TrimsAndRemovesDuplicateOccupationIds() =>
        JobSearchQuery.Create([Developer, $" {Developer} ", Tester], 10).Value
            .OccupationIds.ShouldBe([Developer, Tester]);

    [Fact]
    public void Create_WithNullOccupations_FailsWithNoOccupations() =>
        JobSearchQuery.Create(null, 10).Error.ShouldBe(JobSearchQueryErrors.NoOccupations);

    [Fact]
    public void Create_WithEmptyOccupations_FailsWithNoOccupations() =>
        JobSearchQuery.Create([], 10).Error.ShouldBe(JobSearchQueryErrors.NoOccupations);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankOccupationId_FailsWithOccupationIdEmpty(string? id) =>
        JobSearchQuery.Create([Developer, id], 10).Error.ShouldBe(JobSearchQueryErrors.OccupationIdEmpty);

    [Theory]
    [InlineData(1)]
    [InlineData(JobSearchQuery.MaxResultsLimit)]
    public void Create_WithMaxResultsAtBoundary_Succeeds(int maxResults) =>
        JobSearchQuery.Create([Developer], maxResults).IsSuccess.ShouldBeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(JobSearchQuery.MaxResultsLimit + 1)]
    public void Create_WithMaxResultsOutOfRange_FailsWithMaxResultsOutOfRange(int maxResults) =>
        JobSearchQuery.Create([Developer], maxResults).Error.ShouldBe(JobSearchQueryErrors.MaxResultsOutOfRange);
}
