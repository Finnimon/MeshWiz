using System.Diagnostics.Contracts;

namespace MeshWiz.Utility;

public readonly struct ExceptionResult() : IResult<ExceptionResult, Exception>
{
    
    /// <inheritdoc />
    public bool IsSuccess => Info is NoException;

    /// <inheritdoc />
    public bool IsFailure => Info is not NoException;

    /// <inheritdoc />
    public Exception Info { get; private init; } = NoException.Instance;

    [Pure]
    public static ExceptionResult Success() => new();

    [Pure]
    public static ExceptionResult Failure(Exception e) => e;

    [Pure]
    public static implicit operator bool(ExceptionResult result) => result.IsSuccess;
    [Pure]
    public static implicit operator ExceptionResult(Exception result) => new() { Info = result };

    /// <inheritdoc />
    public bool Equals(ExceptionResult other)
        => this == other;

    /// <inheritdoc />
    public static bool operator ==(ExceptionResult left, ExceptionResult right)
        => left.Info == right.Info;

    /// <inheritdoc />
    public static bool operator !=(ExceptionResult left, ExceptionResult right) => left.Info != right.Info;
    
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExceptionResult other && this == other;

    /// <inheritdoc />
    public override int GetHashCode()
        => Info?.GetHashCode() ?? 0;
}


public readonly struct ExceptionResult<T> : IValueResult<ExceptionResult<T>, Exception, T>
{
    private readonly object? _value;

    /// <inheritdoc />
    public bool IsSuccess { get; private init; }

    /// <inheritdoc />
    public bool IsFailure => !IsSuccess;

    /// <inheritdoc />
    public bool HasValue => IsSuccess;

    /// <inheritdoc />
    public Exception Info
    {
        get => IsSuccess ? NoException.Instance : (Exception)_value!;
        private init => _value = value;
    }

    public T Value
    {
        get => IsSuccess ? (T)_value! : throw Info;
        private init => _value = value;
    }

    [Pure]
    public static ExceptionResult<T> Success(T value) => new() { IsSuccess = true, Value = value };

    [Pure]
    public static ExceptionResult<T> Failure(Exception e) => new() { IsSuccess = false, Info = e };

    [Pure]
    public static implicit operator bool(in ExceptionResult<T> result) => result.IsSuccess;

    public static implicit operator T(in ExceptionResult<T> result) => result.Value;
    public static implicit operator ExceptionResult<T>(T result) => Success(result);
    public static implicit operator ExceptionResult<T>(Exception ex)=>Failure(ex);

    /// <inheritdoc />
    public bool Equals(ExceptionResult<T> other)
        => this == other;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ExceptionResult<T> other && this == other;

    /// <inheritdoc />
    public static bool operator ==(ExceptionResult<T> left, ExceptionResult<T> right) =>    
        
        left.IsSuccess == right.IsSuccess && (left._value?.Equals(right._value) ?? right._value is null);

    /// <inheritdoc />
    public static bool operator !=(ExceptionResult<T> left, ExceptionResult<T> right)
        => left.IsSuccess != right.IsSuccess || !(left._value?.Equals(right._value) ?? right._value is null);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_value, IsSuccess);
}

internal sealed class NoException : Exception
{
    public static readonly NoException Instance = new();
    private NoException() : base("No Exception") { }
}