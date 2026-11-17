using Jobpilot.Api.Endpoints;
using Jobpilot.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Jobpilot.Api.IntegrationTests;

public class ResultHttpExtensionsTests
{
    [Theory]
    [InlineData(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ErrorType.Unavailable, StatusCodes.Status503ServiceUnavailable)]
    [InlineData((ErrorType)999, StatusCodes.Status500InternalServerError)]
    public void Failure_MapsTheErrorTypeToAStatus_AndKeepsTheCode(ErrorType type, int status)
    {
        var result = Result.Failure<int>(new Error("Some.Code", "Something went wrong.", type)).ToHttpResult();

        var problem = result.ShouldBeOfType<ProblemHttpResult>();
        problem.StatusCode.ShouldBe(status);
        problem.ProblemDetails.Title.ShouldBe("Some.Code");
        problem.ProblemDetails.Detail.ShouldBe("Something went wrong.");
        problem.ProblemDetails.Extensions["code"].ShouldBe("Some.Code");
    }

    [Fact]
    public void Success_Is200WithTheValue() =>
        Result.Success(42).ToHttpResult().ShouldBeOfType<Ok<int>>().Value.ShouldBe(42);
}
