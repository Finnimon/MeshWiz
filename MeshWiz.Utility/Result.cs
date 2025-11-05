using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using CommunityToolkit.Diagnostics;

namespace MeshWiz.Results;

public readonly struct Result<TValue, TInfo> : IEquatable<Result<TValue, TInfo>>,
    IEqualityOperators<Result<TValue, TInfo>, Result<TValue, TInfo>, bool>
    where TInfo : unmanaged
{
    public static readonly TInfo SuccessConstant;
    public static readonly TInfo DefaultFailureConstant;
    public static readonly Result<TValue, TInfo> DefaultFailure;

    [field: AllowNull, MaybeNull]
    public TValue Value => field ?? ThrowHelper.ThrowInvalidOperationException<TValue>("Illegal result access");

    public readonly TInfo Info;
    public bool HasValue => Info.Equals(SuccessConstant);
    public bool IsSuccess => HasValue;
    public bool IsFailure => !HasValue;

    private Result(TValue? value, TInfo info)
    {
        if (value is not null)
            Value = value;
        Info = info;
    }

    public Result() => Info = DefaultFailureConstant;

    public static implicit operator TValue(Result<TValue, TInfo> result) => result.Value;
    public static Result<TValue, TInfo> Success(TValue value) => new(value, SuccessConstant);

    public static Result<TValue, TInfo> Failure(TInfo info)
    {
        if (info.Equals(SuccessConstant))
            ThrowHelper.ThrowArgumentOutOfRangeException(nameof(info), info, "Failure info may not be success value");
        return new Result<TValue, TInfo>(default, info);
    }


    static Result()
    {
        var invalidType = false;
        var type = typeof(TInfo);
        try
        {
            SuccessConstant = default;
            DefaultFailureConstant = (TInfo)(object)1;
            if (type.IsAssignableTo(typeof(Enum)))
                invalidType |= !Enum.IsDefined(type, DefaultFailureConstant);
            DefaultFailure = new Result<TValue, TInfo>();
        }
        catch
        {
            invalidType = true;
        }

        if (!invalidType)
            return;
        ThrowHelper.ThrowNotSupportedException(GetBadTypeMessage());
    }

    private static string GetBadTypeMessage() => $"The type {typeof(TInfo)} is not supported. Int32 must be explicitly convertible to {nameof(TInfo)} and 1 must be defined for enums.";

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
        return left.IsSuccess && right.IsSuccess && left.Value!.Equals(right.Value!)
            || left.Info.Equals(right.Info);
    }

    /// <inheritdoc />
    public static bool operator !=(Result<TValue, TInfo> left, Result<TValue, TInfo> right)
    => !(left == right);
}