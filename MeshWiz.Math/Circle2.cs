using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Circle2<TNum> : ISurface<Vector2<TNum>, TNum>,IContiguousDiscreteCurve<Vector2<TNum>,TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector2<TNum> Center;
    public readonly TNum Radius;
    public TNum Diameter => Radius * Numbers<TNum>.Two;
    public TNum Circumference => Radius * Numbers<TNum>.TwoPi;
    public TNum SurfaceArea => Radius * Radius * TNum.Pi;

    public Circle2(Vector2<TNum> center, TNum radius)
    {
        Center = center;
        Radius = radius;
    }

    public Vector2<TNum> Centroid => Center;

    public Vector2<TNum> TraverseByAngle(TNum angle)
    {
        var (sin, cos) = TNum.SinCos(angle);
        Vector2<TNum> dir = new(cos, sin);
        return Center + dir * Radius;
    }

    public Vector2<TNum> U => new(Radius, TNum.Zero);
    public Vector2<TNum> V => new(TNum.Zero, Radius);

    /// <inheritdoc />
    public Vector2<TNum> Traverse(TNum distance)
        => TraverseByAngle(distance * Numbers<TNum>.TwoPi);

    /// <inheritdoc />
    public Vector2<TNum> GetTangent(TNum at)
    {
        var angle=at*Numbers<TNum>.TwoPi;
        var dirAngle= Numbers<TNum>.HalfPi+angle;
        return new Vector2<TNum>(TNum.One, dirAngle).PolarToCartesian();
    }

    /// <inheritdoc />
    public Vector2<TNum> Start => U * Radius;

    /// <inheritdoc />
    public Vector2<TNum> End => Start;

    /// <inheritdoc />
    public Vector2<TNum> TraverseOnCurve(TNum distance)
        => Traverse(distance);

    /// <inheritdoc />
    public TNum Length => Circumference;

    /// <inheritdoc />
    public Polyline<Vector2<TNum>, TNum> ToPolyline()
    {
        const int edgeCount = 32;
        var verts = new Vector2<TNum>[edgeCount + 1];
        var edgeCountNum = TNum.CreateTruncating(edgeCount);
        for (var i = 0; i < verts.Length - 1; i++)
        {
            var angle = TNum.CreateTruncating(i) / edgeCountNum * Numbers<TNum>.TwoPi;
            verts[i] = TraverseByAngle(angle);
        }

        verts[^1] = verts[0];
        return new(verts);
    }

    /// <inheritdoc />
    public Polyline<Vector2<TNum>, TNum> ToPolyline(PolylineTessellationParameter<TNum> tessellationParameter)
    {
        var steps = Numbers<TNum>.TwoPi / tessellationParameter.MaxAngularDeviation;
        steps = TNum.Round(steps, MidpointRounding.AwayFromZero);
        var intSteps = int.CreateTruncating(steps);
        intSteps = int.Abs(intSteps);
        var verts = new Vector2<TNum>[intSteps + 1];
        var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(intSteps);
        var i = -1;
        for (var angle = TNum.Zero; angle < Numbers<TNum>.TwoPi; angle += angleStep) 
            verts[++i] = TraverseByAngle(angle);

        verts[^1] = verts[0];
        return new(verts);
    }

    /// <inheritdoc />
    public Vector2<TNum> EntryDirection => V;

    /// <inheritdoc />
    public Vector2<TNum> ExitDirection => V;
}