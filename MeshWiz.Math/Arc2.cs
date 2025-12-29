using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Arc2<TNum> : IContiguousDiscreteCurve<Vec2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec2<TNum> CircumCenter;
    public readonly TNum Radius, StartAngle, EndAngle;
    public bool IsCircle => AngleRangeSize.IsApprox(Numbers<TNum>.TwoPi);
    public bool IsAtLeastFullCircle => AngleRangeSize.IsApproxGreaterOrEqual(Numbers<TNum>.TwoPi);

    public Arc2(Circle2<TNum> c, TNum startAngle, TNum endAngle)
    {
        CircumCenter = c.Centroid;
        Radius = c.Radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    public Arc2(Vec2<TNum> circumCenter, TNum radius, TNum startAngle, TNum endAngle)
    {
        CircumCenter = circumCenter;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    public Circle2<TNum> Underlying
    {
        get
        {
            var copy = this;
            return Unsafe.As<Arc2<TNum>, Circle2<TNum>>(ref copy);
        }
    }

    public TNum AngleRangeSize => TNum.Abs(EndAngle - StartAngle);
    public TNum SignedAngleRange => EndAngle - StartAngle;
    public AABB<TNum> AngleBoundary => AABB.From(StartAngle, EndAngle);

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
    public Vec2<TNum> Start => Traverse(TNum.Zero);

    /// <inheritdoc />
    public Vec2<TNum> End => Traverse(TNum.One);

    /// <inheritdoc />
    public Vec2<TNum> TraverseOnCurve(TNum t)
        => Traverse(TNum.Clamp(t, TNum.Zero, TNum.One));

    /// <inheritdoc />
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
    public Vec2<TNum> EntryDirection => GetTangent(TNum.Zero);

    /// <inheritdoc />
    public Vec2<TNum> ExitDirection => GetTangent(TNum.One);
}