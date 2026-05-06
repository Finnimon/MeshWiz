using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public sealed class Polygon2<TPolyline, TNum> : ISurface<Vec2<TNum>, TNum>, IBounded<Vec2<TNum>>
    where TPolyline : IPolyline<TPolyline, Line<Vec2<TNum>, TNum>, Vec2<TNum>, Vec2<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TPolyline Boundary;

    [field: AllowNull, MaybeNull]
    public BvhPolyline<Vec2<TNum>, TNum> CalculationBoundary => field ??= CreateCalculationBoundary();

    private TNum? _signedArea;
    public TNum SignedArea => _signedArea ??= CentroidAndArea().Z;
    public int AreaSign => TNum.Sign(SignedArea);
    public TNum SurfaceArea => TNum.Abs(SignedArea);
    private Vec2<TNum>? _centroid;
    public Vec2<TNum> Centroid => _centroid ??= CentroidAndArea().XY;
    private bool? _isConvex;
    public bool IsConvex => _isConvex ??= Polyline.Evaluate.IsConvex<TPolyline, TNum>(Boundary);

    private Polyline.Simplicity.Level? _simplicity;

    public Polyline.Simplicity.Level Simplicity => _simplicity ??= _isConvex is true
        ? Polyline.Simplicity.Level.Simple
        : Polyline.Simplicity.MultiCheck<TPolyline, TNum>(Boundary);

    public bool IsComplex => Simplicity is Polyline.Simplicity.Level.Complex;
    public bool IsSimple => Simplicity is Polyline.Simplicity.Level.Simple;

    /// <inheritdoc />
    public AABB<Vec2<TNum>> BBox => Boundary.BBox;


    public WindingOrder WindingOrder => AreaSign switch
    {
        -1 => WindingOrder.Clockwise,
        0 => WindingOrder.NotClosed,
        1 => WindingOrder.CounterClockwise,
        _ => ThrowHelper.ThrowArgumentException<WindingOrder>(nameof(AreaSign))
    };


    private Polygon2(TPolyline boundary) => Boundary = boundary;

    public static Result<Arithmetics, Polygon2<TPolyline, TNum>> Create(TPolyline polyline) =>
        Result<Arithmetics, TPolyline>.Success(polyline)
            .When(pl => pl.IsClosed)
            .Select(pl => new Polygon2<TPolyline, TNum>(pl));


    private Vec3<TNum> CentroidAndArea()
    {
        var vertices = Boundary.Vertices;
        var n = vertices.Count;
        var signedArea = TNum.Zero;
        var centroid = Vec2<TNum>.Zero;

        var prev = vertices[0];
        for (var i = 1; i < n; i++)
        {
            var cur = vertices[i];

            var cross = Vec2<TNum>.Cross(prev, cur);

            signedArea += cross;
            centroid += (prev + cur) * cross;
            prev = cur;
        }

        signedArea *= Numbers<TNum>.Half;

        centroid /= TNum.CreateChecked(6) * signedArea;

        _centroid = centroid;
        _signedArea = signedArea;

        return Vec3<TNum>.Create(centroid, signedArea);
    }

    private BvhPolyline<Vec2<TNum>, TNum> CreateCalculationBoundary() => Boundary as BvhPolyline<Vec2<TNum>, TNum> ??
                                                                         BvhPolyline<Vec2<TNum>, TNum>.BinaryBalanced(
                                                                             Boundary.ToPolyline());

    public IntersectionLevel ContainsPoint(Vec2<TNum> p)
    {
        TNum hit = default;
        var hitC = 0;
        BvhPolyline.ContainsPoint<TNum> trav = new(p, ref hit, ref hitC);
        var anyHit = CalculationBoundary.TraverseBvh<BvhPolyline.ContainsPoint<TNum>, TNum>(trav);
        return trav.FinalEval(CalculationBoundary.Underlying, WindingOrder);
    }

    public bool Hit(Ray2<TNum> ray)
    {
        Unsafe.SkipInit(out TNum hit);
        var traverser = new BvhPolyline.AnyHit<TNum>(ray, ref hit);
        return CalculationBoundary.TraverseBvh<BvhPolyline.AnyHit<TNum>, TNum>(traverser);
    }

    public bool Hits<TCol>(Ray2<TNum> ray, TCol buffer)
        where TCol : ICollection<TNum>
    {
        var traverser = new BvhPolyline.AllHits<TCol, TNum>(ray, buffer);
        return CalculationBoundary.TraverseBvh<BvhPolyline.AllHits<TCol, TNum>, TNum>(traverser);
    }
}