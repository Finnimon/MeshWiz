using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility.Extensions;
using static MeshWiz.Utility.Enums;

namespace MeshWiz.Utility;

public readonly struct Result<TValue, TInfo> : IValueResult<Result<TValue, TInfo>, TValue, TInfo>
    where TInfo : unmanaged, Enum
{
    public static readonly Result<TValue, TInfo> DefaultFailure = Failure(ResultHelper<TInfo>.DefaultFailureConstant);

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

    public Result() => Info = ResultHelper<TInfo>.DefaultFailureConstant;

    public static implicit operator TValue(Result<TValue, TInfo> result) => result.Value;
    public static Result<TValue, TInfo> Success(TValue value) => new(value, ResultHelper<TInfo>.SuccessConstant);

    public static Result<TValue, TInfo> Failure(TInfo info)
    {
        if (info.Equals(ResultHelper<TInfo>.SuccessConstant))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(info), info, "Failure info may not be success value");
        return new Result<TValue, TInfo>(default, info);
    }

    public static Result<TValue, TInfo> Failure()
        => DefaultFailure;

    /// <inheritdoc />
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is Result<TValue, TInfo> other && other == this;

    /// <inheritdoc />
    public override int GetHashCode() => HasValue ? Value!.GetHashCode() : Info.GetHashCode();

    /// <inheritdoc />
    public bool Equals(Result<TValue, TInfo> other)
        => this == other;

    /// <inheritdoc />
    public static bool operator ==(Result<TValue, TInfo> left, Result<TValue, TInfo> right)
    {
        var sameInfo = AreEqual(left.Info, right.Info);
        return left.HasValue && right.HasValue ? sameInfo && left.Value!.Equals(right.Value!) : sameInfo;
    }

    /// <inheritdoc />
    public static bool operator !=(Result<TValue, TInfo> left, Result<TValue, TInfo> right)
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
    public bool IsSuccess => ResultHelper<TInfo>.SuccessConstant.Equals(Info);

    /// <inheritdoc />
    public bool IsFailure => !ResultHelper<TInfo>.SuccessConstant.Equals(Info);

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
}