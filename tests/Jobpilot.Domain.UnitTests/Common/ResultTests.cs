using Jobpilot.Domain.Common;

namespace Jobpilot.Domain.UnitTests.Common;

public class ResultTests
{
    private static readonly Error _error = Error.Validation("Test.Error", "Something went wrong.");

    [Fact]
    public void Success_IsSuccessful()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
    }

    [Fact]
    public void Failure_IsFailedWithError()
    {
        var result = Result.Failure(_error);

        result.IsFailure.ShouldBeTrue();
        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(_error);
    }

    [Fact]
    public void Failure_WithNullError_Throws() =>
        Should.Throw<ArgumentNullException>(() => Result.Failure(null!));

    [Fact]
    public void Error_OnSuccess_Throws() =>
        Should.Throw<InvalidOperationException>(() => Result.Success().Error);

    [Fact]
    public void SuccessOfT_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void SuccessOfT_WithNullValue_Throws() =>
        Should.Throw<ArgumentNullException>(() => Result.Success<string>(null!));

    [Fact]
    public void FailureOfT_IsFailedWithError()
    {
        var result = Result.Failure<int>(_error);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_error);
    }

    [Fact]
    public void Value_OnFailure_ThrowsWithErrorCode()
    {
        var result = Result.Failure<int>(_error);

        var exception = Should.Throw<InvalidOperationException>(() => result.Value);
        exception.Message.ShouldContain("Test.Error");
    }

    [Fact]
    public void ImplicitConversion_FromValue_CreatesSuccess()
    {
        Result<string> result = "hello";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    [Fact]
    public void ImplicitConversion_FromErrorToResultOfT_CreatesFailure()
    {
        Result<string> result = _error;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_error);
    }

    [Fact]
    public void ImplicitConversion_FromErrorToResult_CreatesFailure()
    {
        Result result = _error;

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(_error);
    }
}
