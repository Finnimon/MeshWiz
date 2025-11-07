using System.Diagnostics.Contracts;
using System.Numerics;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct
    ConeSection<TNum> : IBody<TNum>, IRotationalSurface<TNum> // ,IGeodesicProvider<ConicalHelicoid<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Line<Vector3<TNum>, TNum> Axis;
    public readonly TNum BaseRadius;
    public readonly TNum TopRadius;
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, BaseRadius);
    public Circle3<TNum> Top => new(Axis.End, Axis.Direction, TopRadius);

    public bool IsComplex
    {
        get
        {
            var baseSign = BaseRadius.EpsilonTruncatingSign();
            var topSign = TopRadius.EpsilonTruncatingSign();
            return baseSign != topSign && baseSign != 0 && topSign != 0;
        }
    }

    public bool IsSimple => TNum.Sign(BaseRadius) == TNum.Sign(TopRadius);
    public bool IsCylinder => BaseRadius == TopRadius;

    public TNum SlantHeight
    {
        get
        {
            var rDiff = BaseRadius - TopRadius;
            return TNum.Sqrt(rDiff * rDiff + Axis.SquaredLength);
        }
    }


    public ConeSection(Line<Vector3<TNum>, TNum> axis, TNum baseRadius, TNum topRadius)
    {
        Axis = axis;
        BaseRadius = baseRadius;
        TopRadius = topRadius;
    }

    public bool TryGetComplete(out Cone<TNum> cone)
    {
        cone = default;
        var failed = !Solver.Linear.TrySolveForZero(BaseRadius, TopRadius, out var at);
        if (failed) return false;
        var tip = Axis.Traverse(at);
        var coneBase = !BaseRadius.IsApprox(TNum.Zero) ? Base : Top;
        cone = new Cone<TNum>(coneBase.Centroid.LineTo(tip), coneBase.Radius);
        return true;
    }

    public bool TryGetInflection(out Vector3<TNum> inflectionPoint)
    {
        inflectionPoint = default;
        var failed = !Solver.Linear.TrySolveForZero(BaseRadius, TopRadius, out var at);
        if (failed) return false;
        inflectionPoint = Axis.Traverse(TNum.Abs(at));
        return true;
    }

    /// <inheritdoc />
    public Vector3<TNum> Centroid => IsComplex ? ComplexCentroid() : SimpleCentroid();

    private Vector3<TNum> ComplexCentroid()
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();

        var baseCone = new Cone<TNum>(Base.Centroid.LineTo(tip), Base.Radius);
        var topCone = new Cone<TNum>(Top.Centroid.LineTo(tip), Top.Radius);
        var baseV = baseCone.Volume;
        var topV = topCone.Volume;
        return (baseCone.Centroid * baseV + topCone.Centroid * topV) / (baseV + topV);
    }

    private Vector3<TNum> SimpleCentroid()
    {
        var baseRadius = TNum.Abs(BaseRadius);
        var topRadius = TNum.Abs(TopRadius);
        var baseRadiusSq = baseRadius * baseRadius;
        var topRadiusSq = topRadius * topRadius;
        var t = (baseRadiusSq + Numbers<TNum>.Two * baseRadius * topRadius + Numbers<TNum>.Three * topRadiusSq)
                / (Numbers<TNum>.Four * (baseRadiusSq + baseRadius * topRadius + topRadiusSq));
        return Axis.Traverse(t);
    }

    public TNum SlantSurfaceArea => TNum.Pi * (BaseRadius + TopRadius) * SlantHeight;

    /// <inheritdoc />
    public TNum SurfaceArea => SlantSurfaceArea + Top.SurfaceArea + Base.SurfaceArea;

    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox => Base.BBox.CombineWith(Top.BBox);

    /// <inheritdoc />
    public IMesh<TNum> Tessellate()
        => Tessellate(32);

    public IndexedMesh<TNum> Tessellate(int edgeCount)
        => IsComplex ? TessellateComplex(edgeCount) : TessellateCylindrical(Base, Top, edgeCount);


    /// <summary>
    /// assumes <paramref name="baseC"/> and <paramref name="topC"/> face the same direction
    /// </summary>
    public static IndexedMesh<TNum> TessellateCylindrical(Circle3<TNum> baseC, Circle3<TNum> topC, int edgeCount)
    {
        var baseMesh = baseC.Tessellate(edgeCount);
        var topMesh = topC.Tessellate(edgeCount);
        Vector3<TNum>[] vertices = [..baseMesh.Vertices, ..topMesh.Vertices];
        var indices = new TriangleIndexer[edgeCount * 4];
        baseMesh.Indices.CopyTo(indices);
        topMesh.Indices.CopyTo(indices, edgeCount);
        for (var i = 0; i < topMesh.Indices.Length; i++)
        {
            //adjust top
            var topIndex = edgeCount + i;
            var topIndexer = (indices[topIndex] += baseMesh.Vertices.Length);
            var baseIndexer = indices[i];
            //flip base
            indices[i] = (baseIndexer = new(baseIndexer.A, baseIndexer.C, baseIndexer.B));
            //create sideWall
            var sideWallBaseIndex = topIndex + edgeCount;
            var sideWallTopIndex = sideWallBaseIndex + edgeCount;
            //quadIndices
            var a = topIndexer.C;
            var b = topIndexer.B;
            var c = baseIndexer.C;
            var d = baseIndexer.B;
            indices[sideWallBaseIndex] = new(a, c, d);
            indices[sideWallTopIndex] = new(a, b, c);
        }

        return new IndexedMesh<TNum>(vertices, indices);
    }

    private IndexedMesh<TNum> TessellateComplex(int edgeCount)
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        var baseMesh = new Cone<TNum>(Base.Centroid.LineTo(tip), Base.Radius).Tessellate(edgeCount);
        var topMesh = new Cone<TNum>(Top.Centroid.LineTo(tip), Top.Radius).Tessellate(edgeCount);
        Vector3<TNum>[] vertices = [..baseMesh.Vertices, ..topMesh.Vertices]; //acceptable duplication of tip
        var shift = vertices.Length / 2;
        TriangleIndexer[] indices = [..baseMesh.Indices, ..topMesh.Indices.Select(i => i + shift)];
        return new(vertices, indices);
    }

    /// <inheritdoc />
    public TNum Volume => IsComplex ? ComplexVolume() : SimpleVolume();

    private TNum SimpleVolume()
    {
        if (IsCylinder) return new Cylinder<TNum>(Axis, BaseRadius).Volume;
        return Numbers<TNum>.Third * TNum.Pi * Height *
               TNum.Sqrt(BaseRadius * BaseRadius + BaseRadius * TopRadius + TopRadius * TopRadius);
    }

    public TNum Height => Axis.Length;

    private TNum ComplexVolume()
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        return new Cone<TNum>(Base.Centroid.LineTo(tip), Base.Radius).Volume
               + new Cone<TNum>(Top.Centroid.LineTo(tip), Top.Radius).Volume;
    }

    /// <inheritdoc />
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve => IsComplex ? ComplexSweepCurve() : SimpleSweepCurve();

    private Polyline<Vector3<TNum>, TNum> ComplexSweepCurve()
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        //both forced same up for same 0 angle result
        var baseRad = TNum.Abs(BaseRadius);
        var topRad = TNum.Abs(TopRadius);
        var normal = Axis.Direction; //gets normalized in ctor
        var start = new Circle3<TNum>(Axis.Start, normal, baseRad).TraverseByAngle(TNum.Zero);
        var end = new Circle3<TNum>(Axis.End, normal, topRad).TraverseByAngle(TNum.Zero);
        return new Polyline<Vector3<TNum>, TNum>(start, tip, end);
    }

    private Line<Vector3<TNum>, TNum> SimpleSweepCurve()
        => Base.TraverseByAngle(TNum.Zero).LineTo(Top.TraverseByAngle(TNum.Zero));

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis.Start.RayThrough(Axis.End);

    /// <inheritdoc />
    public Vector3<TNum> NormalAt(Vector3<TNum> p)
    {
        p = ClampToSurface(p);
        return this.TryGetComplete(out var cone)
            ? cone.NormalAt(p)
            : new Cylinder<TNum>(Axis, BaseRadius).NormalAt(p);
    }
    //
    // /// <inheritdoc />
    // public ConicalHelicoid<TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
    // {
    //     return ConicalHelicoid<TNum>.BetweenPoints(in this, p1, p2);
    // }
    //
    // /// <inheritdoc />
    // public ConicalHelicoid<TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
    // {
    //     return ConicalHelicoid<TNum>.FromEntry(in this, entryPoint, direction);
    // }
    [Pure]
    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        var (closest, onseg) = Axis.ClosestPoints(p);
        var vShift = onseg - closest;
        p += vShift;
        var pos = onseg.DistanceTo(Axis.Start) / Axis.Length;
        var radius = TNum.Abs(RadiusAt(pos));
        return Vector3<TNum>.ExactLerp(onseg, p, radius);
    }
    
    
    [Pure]
    public TNum RadiusAt(TNum pos) => TNum.Lerp(BaseRadius, TopRadius, pos);

    /// <inheritdoc />
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesic(Vector3<TNum> p1, Vector3<TNum> p2)
    {
        ValidateForGeodesics();
        if (IsCylinder) return new Cylinder<TNum>(Axis, TNum.Abs(BaseRadius)).GetGeodesic(p1, p2);
        return ConeGeodesic<TNum>.BetweenPoints(
            MakeUpright(in this).TryGetComplete(out var surface)
                ? surface
                : ThrowHelper.ThrowInvalidOperationException<Cone<TNum>>(),
            ClampToSurface(p1), ClampToSurface(p2));
    }

    private void ValidateForGeodesics()
    {
        if (!CanGeodesic)
            ThrowHelper.ThrowInvalidOperationException("Geodesics are only possible on non complex ConeSections");
    }

    private bool CanGeodesic => !IsComplex;

    /// <inheritdoc />
    public IContiguousCurve<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
    {
        ValidateForGeodesics();
        if (IsCylinder)
            return new Cylinder<TNum>(Axis, TNum.Abs(BaseRadius)).GetGeodesicFromEntry(entryPoint, direction);
        return ConeGeodesic<TNum>.FromDirection(surface: MakeUpright(this), entryPoint, direction);
    }

    public static ConeSection<TNum> MakeUpright(in ConeSection<TNum> surf)
    {
        if (surf.IsComplex)
            ThrowHelper.ThrowInvalidOperationException();
        var sign = surf.BaseRadius.EpsilonTruncatingSign();
        if (sign == 0) sign = surf.TopRadius.EpsilonTruncatingSign();
        var absBase = TNum.Abs(surf.BaseRadius);
        var absTop = TNum.Abs(surf.TopRadius);
        var reverse = sign == -1;
        reverse ^= absBase < absTop;
        if (!reverse && sign == 1) return surf;
        if(!reverse)
            return new(surf.Axis,absBase,absTop);
        var axis =surf.Axis.Reversed();
        return new ConeSection<TNum>(axis, absTop,absBase);
    }
}
