namespace Jobpilot.Domain.Common;

/// <summary>Outcome of an operation that can fail in an expected way. Use instead of exceptions for such cases.</summary>
public class Result
{
    private readonly Error? _error;

    private protected Result() => IsSuccess = true;

    private protected Result(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        _error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    /// <summary>The failure reason. Reading it on a successful result is a bug and throws.</summary>
    public Error Error => _error ?? throw new InvalidOperationException("A successful result has no error.");

    public static Result Success() => new();

    public static Result<T> Success<T>(T value) => new(value);

    public static Result Failure(Error error) => new(error);

    public static Result<T> Failure<T>(Error error) => new(error);

    public static implicit operator Result(Error error) => Failure(error);
}

/// <summary>Outcome of an operation that returns a value on success.</summary>
/// <typeparam name="T">Type of the value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    internal Result(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        _value = value;
    }

    internal Result(Error error)
        : base(error)
    {
    }

    /// <summary>The value. Reading it on a failed result is a bug and throws.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"A failed result has no value. Error: {Error.Code}");

    public static implicit operator Result<T>(T value) => new(value);

    public static implicit operator Result<T>(Error error) => new(error);
}
