namespace FijiPayroll.Application.Common.Models;

/// <summary>
/// Generic result type used as the return value for all Application layer operations.
/// Avoids exceptions crossing layer boundaries. Commands return <see cref="Result"/>,
/// queries return <see cref="Result{T}"/>.
/// </summary>
public class Result
{
    /// <summary>Initialises a new result.</summary>
    protected Result(bool isSuccess, IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Errors    = errors;
    }

    /// <summary>
    /// <c>true</c> when the operation completed successfully without errors.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// <c>true</c> when the operation produced one or more errors.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Collection of error messages. Empty when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>Convenience accessor — first error message, or empty string.</summary>
    public string Error => Errors.Count > 0 ? Errors[0] : string.Empty;

    /// <summary>Creates a successful <see cref="Result"/>.</summary>
    public static Result Success() => new(true, []);

    /// <summary>Creates a failed <see cref="Result"/> with a single error message.</summary>
    public static Result Failure(string error) => new(false, [error]);

    /// <summary>Creates a failed <see cref="Result"/> with multiple error messages.</summary>
    public static Result Failure(IReadOnlyList<string> errors) => new(false, errors);
}

/// <summary>
/// Generic result type carrying a value payload on success.
/// Used by query handlers to return data without throwing exceptions.
/// </summary>
/// <typeparam name="T">The type of the returned value.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _value;

    private Result(bool isSuccess, T? value, IReadOnlyList<string> errors)
        : base(isSuccess, errors)
    {
        _value = value;
    }

    /// <summary>
    /// The returned value. Only valid when <see cref="Result.IsSuccess"/> is <c>true</c>.
    /// Accessing this when <see cref="Result.IsFailure"/> is <c>true</c> throws.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if result is a failure.</exception>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed Result.");

    /// <summary>Creates a successful <see cref="Result{T}"/> carrying a value.</summary>
    public static Result<T> Success(T value) => new(true, value, []);

    /// <summary>Creates a failed <see cref="Result{T}"/> with a single error message.</summary>
    public new static Result<T> Failure(string error) => new(false, default, [error]);

    /// <summary>Creates a failed <see cref="Result{T}"/> with multiple error messages.</summary>
    public new static Result<T> Failure(IReadOnlyList<string> errors) => new(false, default, errors);
}
