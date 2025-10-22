using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct Arc2<TNum> : IContiguousDiscreteCurve<Vector2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector2<TNum> CircumCenter;
    public readonly TNum Radius, StartAngle, EndAngle;

    public Arc2(Circle2<TNum> c, TNum startAngle, TNum endAngle)
    {
        CircumCenter = c.Centroid;
        Radius = c.Radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    public Arc2(Vector2<TNum> circumCenter, TNum radius, TNum startAngle, TNum endAngle)
    {
        CircumCenter = circumCenter;
        Radius = radius;
        StartAngle = startAngle;
        EndAngle = endAngle;
    }

    public Circle2<TNum> Underlying => new(CircumCenter, Radius);
    public TNum AngleRangeSize => TNum.Abs(EndAngle - StartAngle);
    public TNum SignedAngleRange => EndAngle - StartAngle;
    public AABB<TNum> AngleRange => AABB.From(StartAngle, EndAngle);

    public TNum WindingDirection => (EndAngle - StartAngle).EpsilonTruncatingSign() switch
    {
        -1 => TNum.NegativeOne,
        0 => TNum.Zero,
        1 => TNum.One,
        _ => ThrowHelper.ThrowArgumentOutOfRangeException<TNum>()
    };

    /// <inheritdoc />
    public Vector2<TNum> Traverse(TNum distance)
    {
        var angle = TNum.Lerp(StartAngle, EndAngle, distance);
        return Underlying.TraverseByAngle(angle);
    }


    /// <inheritdoc />
    public Vector2<TNum> GetTangent(TNum at)
    {
        var angle = TNum.Lerp(StartAngle, EndAngle, at);
        var underlyingAt = angle / AngleRangeSize;
        return Underlying.GetTangent(underlyingAt);
    }


    /// <inheritdoc />
    public Vector2<TNum> Start => Traverse(TNum.Zero);

    /// <inheritdoc />
    public Vector2<TNum> End => Traverse(TNum.One);

    /// <inheritdoc />
    public Vector2<TNum> TraverseOnCurve(TNum distance)
        => Traverse(TNum.Clamp(distance, TNum.Zero, TNum.One));

    /// <inheritdoc />
    public TNum Length => AngleRangeSize / Numbers<TNum>.TwoPi * Underlying.Circumference;

    /// <inheritdoc />
    public Polyline<Vector2<TNum>, TNum> ToPolyline()
        => ToPolyline(new PolylineTessellationParameter<TNum> { MaxAngularDeviation = Numbers<TNum>.Eps2 });

    /// <inheritdoc />
    public Polyline<Vector2<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var (count, countNum, stepSize) = tessellationParameter.GetStepsForAngle(SignedAngleRange);
        var verts=new Vector2<TNum>[count+1];
        var step = TNum.One/countNum;
        var pos = TNum.Zero;
        
        for (var i = 0; i < verts.Length; i++)
        {
            verts[i] = Traverse(step);
            pos += step;
        }

        return new Polyline<Vector2<TNum>, TNum>(verts);
    }

    /// <inheritdoc />
    public Vector2<TNum> EntryDirection => GetTangent(TNum.Zero);

    /// <inheritdoc />
    public Vector2<TNum> ExitDirection => GetTangent(TNum.One);
}