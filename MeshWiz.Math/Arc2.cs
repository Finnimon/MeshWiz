using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Arc2<TNum> : IContiguousDiscreteCurve<Vec2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    [JsonInclude]
    public readonly Vec2<TNum> CircumCenter;
    [JsonInclude]
    public readonly TNum Radius, StartAngle, EndAngle;
    [JsonIgnore]
    public bool IsCircle => AngleRangeSize.IsApprox(Numbers<TNum>.TwoPi);
    [JsonIgnore]
    public bool IsAtLeastFullCircle => AngleRangeSize.IsApproxGreaterOrEqual(Numbers<TNum>.TwoPi);

    public Arc2(Circle2<TNum> c, TNum startAngle, TNum endAngle)
    {
        CircumCenter = c.Centroid;
        Radius = c.Radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    [JsonConstructor]
    public Arc2(Vec2<TNum> circumCenter, TNum radius, TNum startAngle, TNum endAngle)
    {
        CircumCenter = circumCenter;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    [JsonIgnore]
    public Circle2<TNum> Underlying
    {
        get
        {
            var copy = this;
            return Unsafe.As<Arc2<TNum>, Circle2<TNum>>(ref copy);
        }
    }

    [JsonIgnore]
    public TNum AngleRangeSize => TNum.Abs(EndAngle - StartAngle);
    [JsonIgnore]
    public TNum SignedAngleRange => EndAngle - StartAngle;
    [JsonIgnore]
    public AABB<TNum> AngleBoundary => AABB.From(StartAngle, EndAngle);

    [JsonIgnore]
    public TNum WindingDirection => (EndAngle - StartAngle).EpsilonTruncatingSign() switch
    {
        -1 => TNum.NegativeOne,
        0 => TNum.Zero,
        1 => TNum.One,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<TNum>()
    };

    /// <inheritdoc />
    public Vec2<TNum> Traverse(TNum t)
    {
        var angle = TNum.Lerp(StartAngle, EndAngle, t);
        return Underlying.TraverseByAngle(angle);
    }


    /// <inheritdoc />
    public Vec2<TNum> GetTangent(TNum t)
    {
        var angle = TNum.Lerp(StartAngle, EndAngle, t);
        var underlyingAt = angle / AngleRangeSize;
        return Underlying.GetTangent(underlyingAt);
    }


    /// <inheritdoc />
    [JsonIgnore]
    public Vec2<TNum> Start => Traverse(TNum.Zero);

    /// <inheritdoc />
    [JsonIgnore]
    public Vec2<TNum> End => Traverse(TNum.One);

    /// <inheritdoc />
    public Vec2<TNum> TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />
    [JsonIgnore]
    public TNum Length => AngleRangeSize / Numbers<TNum>.TwoPi * Underlying.Circumference;

    /// <inheritdoc />
    public Polyline<Vec2<TNum>, TNum> ToPolyline()
        => ToPolyline(new PolylineTessellationParameter<TNum> { MaxAngularDeviation = Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vec2<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var (count, countNum, stepSize) = tessellationParameter.GetStepsForAngle(SignedAngleRange);
        var verts = new Vec2<TNum>[count + 1];
        var step = TNum.One / countNum;
        var pos = TNum.Zero;

        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = Traverse(step);
            pos += step;
        }

        return new Polyline<Vec2<TNum>, TNum>(verts);
    }

    /// <inheritdoc />
    [JsonIgnore]
    public Vec2<TNum> EntryDirection => GetTangent(TNum.Zero);

    /// <inheritdoc />
    [JsonIgnore]
    public Vec2<TNum> ExitDirection => GetTangent(TNum.One);

    [System.Diagnostics.Contracts.Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Arc2<TOther> To<TOther>()
        where TOther : unmanaged, IFloatingPointIeee754<TOther>
        => new(CircumCenter.To<TOther>(), TOther.CreateTruncating(Radius), TOther.CreateTruncating(StartAngle),
            TOther.CreateTruncating(EndAngle));
}