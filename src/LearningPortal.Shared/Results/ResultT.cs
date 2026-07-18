namespace LearningPortal.Shared.Results;

/// <summary>
/// Represents the immutable outcome of an operation that returns a value on success.
/// </summary>
/// <typeparam name="T">The type of the successful value.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, bool isSuccess, Error? error)
    {
        _value = value;
        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets the successful operation value.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when accessed on a failed result.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("A failed result does not contain a value.");

    /// <summary>
    /// Gets the operation error when the operation failed; otherwise, <see langword="null"/>.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Creates a successful result containing the specified value.
    /// </summary>
    /// <param name="value">The successful operation value.</param>
    /// <returns>A successful result containing <paramref name="value"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value, true, null);
    }

    /// <summary>
    /// Creates a failed result containing the specified error.
    /// </summary>
    /// <param name="error">The error that caused the operation to fail.</param>
    /// <returns>A failed result.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    public static Result<T> Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(default, false, error);
    }
}
