using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using static MeshWiz.Utility.Enums;

namespace MeshWiz.Utility;

public readonly struct Result<TInfo, TValue> : IValueResult<Result<TInfo, TValue>, TInfo, TValue>
    where TInfo : unmanaged, Enum
{
    public static readonly Result<TInfo, TValue> DefaultFailure =
        Failure(EnumResultHelper<TInfo>.DefaultFailureConstant);

    [field: AllowNull, MaybeNull]
    public TValue Value => HasValue
        ? field!
        : ThrowHelper.ThrowInvalidOperationException<TValue>("Illegal result access");

    public TInfo Info { get; }
    public bool HasValue => IsSuccess(Info);
    public bool IsSuccess => HasValue;
    public bool IsFailure => !HasValue;

    private Result(TValue? value, TInfo info)
    {
        if (value is not null)
            Value = value;
        Info = info;
    }

    public Result() => Info = EnumResultHelper<TInfo>.DefaultFailureConstant;

    public static implicit operator TValue(Result<TInfo, TValue> result) => result.Value;
    public static implicit operator Result<TInfo, TValue>(TValue value) => Success(value);
    public static Result<TInfo, TValue> Success(TValue value) => new(value, EnumResultHelper<TInfo>.SuccessConstant);

    public static Result<TInfo, TValue> Failure(TInfo info)
    {
        if (info.Equals(EnumResultHelper<TInfo>.SuccessConstant))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(info), info, "Failure info may not be success value");
        return new Result<TInfo, TValue>(default, info);
    }

    public static Result<TInfo, TValue> Failure()
        => DefaultFailure;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Result<TInfo, TValue> other && other == this;

    /// <inheritdoc />
    public override int GetHashCode() => HasValue ? Value!.GetHashCode() : Info.GetHashCode();

    /// <inheritdoc />
    public bool Equals(Result<TInfo, TValue> other)
        => this == other;

    /// <inheritdoc />
    public static bool operator ==(Result<TInfo, TValue> left, Result<TInfo, TValue> right)
    {
        var sameInfo = AreEqual(left.Info, right.Info);
        return left.HasValue && right.HasValue ? sameInfo && left.Value!.Equals(right.Value!) : sameInfo;
    }

    /// <inheritdoc />
    public static bool operator !=(Result<TInfo, TValue> left, Result<TInfo, TValue> right)
        => !(left == right);
}

public readonly struct Result<TInfo> : IResult<Result<TInfo>, TInfo>
    where TInfo : unmanaged, Enum
{
    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is Result<TInfo> other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Info.GetHashCode();

    /// <inheritdoc />
    public bool IsSuccess => EnumResultHelper<TInfo>.SuccessConstant.Equals(Info);

    /// <inheritdoc />
    public bool IsFailure => !EnumResultHelper<TInfo>.SuccessConstant.Equals(Info);

    /// <inheritdoc />
    public TInfo Info { get; }

    private Result(TInfo info) => Info = info;

    /// <inheritdoc />
    public bool Equals(Result<TInfo> other)
        => this == other;

    /// <inheritdoc />
    public static bool operator ==(Result<TInfo> left, Result<TInfo> right)
        => AreEqual(left.Info, right.Info);

    /// <inheritdoc />
    public static bool operator !=(Result<TInfo> left, Result<TInfo> right)
        => !AreEqual(left.Info, right.Info);
    
    public static implicit operator Result<TInfo>(TInfo info)=>Unsafe.As<TInfo,Result<TInfo>>(ref info);
    // ReSharper disable once InconsistentNaming
    private static readonly Result<TInfo> _success = new(EnumResultHelper<TInfo>.SuccessConstant);
    public static Result<TInfo> Success() => _success;
    public static Result<TInfo> Failure() => new(EnumResultHelper<TInfo>.DefaultFailureConstant);

    public static Result<TInfo> Failure(TInfo failInfo)
    {
        EnumResultHelper<TInfo>.ValidateFailureInfo(failInfo);
        return new Result<TInfo>(failInfo);
    }
}

public static class Result
{
    [DoesNotReturn, StackTraceHidden]
    internal static T ThrowIllegalValueAccess<T>()
    {
        const string msg = "Value access on failure Result";
        return ThrowHelper.ThrowInvalidOperationException<T>(msg);
    }

    public static TResult OrElse<TResult, TInfo>(this TResult result, Func<TResult> func)
        where TResult : struct, IResult<TResult, TInfo> =>
        result.IsSuccess ? result : func();

    public static TResult OrElse<TResult, TInfo, TValue>(this TResult result, Func<TValue> func)
        where TResult : struct, IValueResult<TResult, TInfo, TValue> =>
        result.IsSuccess ? result : TResult.Success(func());


    //explicitly typed Overloads for performance gains
    
    public static Result<TInfo> OrElse<TInfo>(in this Result<TInfo> result, Func<Result<TInfo>> func)
        where TInfo : unmanaged, Enum
        => result.IsSuccess ? result : func();

    public static Result<TInfo> OrElse<TInfo>(in this Result<TInfo> result, Func<TInfo> func)
        where TInfo : unmanaged, Enum
        => result.IsSuccess ? result : func();
    
    public static Result<TInfo,TValue> OrElse<TInfo, TValue>(this Result<TInfo,TValue> result, Func<TValue> func)
    where TInfo:unmanaged,Enum
    =>result.IsSuccess ? result : func();

    public static ExceptionResult OrElse(in this ExceptionResult result, Action func)
        => result.IsSuccess ? result : Func.Try(func);

    public static ExceptionResult<T> OrElse<T>(in this ExceptionResult<T> result, Func<T> func)
        => result.IsSuccess ? result : Func.Try(func);
}