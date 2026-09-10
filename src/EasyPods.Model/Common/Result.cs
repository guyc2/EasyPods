namespace EasyPods.Model.Common;

/// <summary>
/// Explicit typed Result pattern to enforce safety and prohibit swallowed exceptions.
/// </summary>
public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly Failure? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException($"Cannot access Value on a failed Result. Error: {_error}");

    public Failure Error => IsFailure 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(T value)
    {
        IsSuccess = true;
        _value = value;
        _error = null;
    }

    private Result(Failure error)
    {
        IsSuccess = false;
        _value = default;
        _error = error ?? throw new ArgumentNullException(nameof(error));
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Fail(Failure error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Failure error) => Fail(error);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Failure, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess(_value!) : onFailure(_error!);
    }
}

public readonly struct Result
{
    private readonly Failure? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public Failure Error => IsFailure 
        ? _error! 
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    private Result(bool isSuccess, Failure? error)
    {
        IsSuccess = isSuccess;
        _error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Fail(Failure error) => new(false, error ?? throw new ArgumentNullException(nameof(error)));

    public static implicit operator Result(Failure error) => Fail(error);

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<Failure, TResult> onFailure)
    {
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        return IsSuccess ? onSuccess() : onFailure(_error!);
    }
}
