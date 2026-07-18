namespace LearningPortal.Shared.Results;

/// <summary>Represents the outcome of an operation that returns no value.</summary>
public class Result
{
    /// <summary>Initializes a result.</summary>
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess == (error is not null))
        {
            throw new ArgumentException("A successful result cannot contain an error and a failed result must contain one.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>Gets whether the operation succeeded.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets whether the operation failed.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the failure, when present.</summary>
    public Error? Error { get; }

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new(true, null);

    /// <summary>Creates a failed result.</summary>
    public static Result Failure(Error error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    /// <summary>Creates a successful result containing a value.</summary>
    public static Result<T> Success<T>(T value) => Result<T>.Success(value);

    /// <summary>Creates a failed result of the requested value type.</summary>
    public static Result<T> Failure<T>(Error error) => Result<T>.Failure(error);
}

/// <summary>Represents the outcome of an operation that returns a value.</summary>
/// <typeparam name="T">The value type.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error? error)
        : base(isSuccess, error) => _value = value;

    /// <summary>Gets the successful value.</summary>
    /// <exception cref="InvalidOperationException">Thrown when the result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result has no value.");

    /// <summary>Creates a successful value result.</summary>
    public static Result<T> Success(T value) => new(value, true, null);

    /// <summary>Creates a failed value result.</summary>
    public new static Result<T> Failure(Error error) => new(default, false, error ?? throw new ArgumentNullException(nameof(error)));
}
