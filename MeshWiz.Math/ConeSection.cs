using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public readonly struct ConeSection<TNum> : IBody<TNum>, IRotationalSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Line<Vector3<TNum>, TNum> Axis;
    public readonly TNum BaseRadius;
    public readonly TNum TopRadius;
    public Circle3<TNum> Base => new(Axis.Start, Axis.Direction, BaseRadius);
    public Circle3<TNum> Top => new(Axis.End, Axis.Direction, TopRadius);

    public bool IsComplex => TNum.Sign(BaseRadius) != TNum.Sign(TopRadius);
    public bool IsSimple => TNum.Sign(BaseRadius) == TNum.Sign(TopRadius);
    public bool IsCylinder => BaseRadius == TopRadius;
    public TNum SlantHeight
    {
        get
        {
            var rDiff=BaseRadius-TopRadius;
            return TNum.Sqrt(rDiff*rDiff+Axis.SquaredLength);
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
        inflectionPoint = Axis.Traverse(at);
        return true;
    }

    /// <inheritdoc />
    public Vector3<TNum> Centroid => IsComplex ? ComplexCentroid() : SimpleCentroid();

    private Vector3<TNum> ComplexCentroid()
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        
        var baseCone=new Cone<TNum>(Base.Centroid.LineTo(tip), Base.Radius);
        var topCone=new Cone<TNum>(Top.Centroid.LineTo(tip), Top.Radius);
        var baseV = baseCone.Volume;
        var topV=topCone.Volume;
        return (baseCone.Centroid * baseV + topCone.Centroid * topV)/(baseV+topV);
    }

    private Vector3<TNum> SimpleCentroid()
    {
        var baseRadius=TNum.Abs(BaseRadius);
        var topRadius=TNum.Abs(TopRadius);
        var baseRadiusSq = baseRadius * baseRadius;
        var topRadiusSq = topRadius * topRadius;
        var t = (baseRadiusSq + Numbers<TNum>.Two * baseRadius * topRadius + Numbers<TNum>.Three * topRadiusSq) 
                / (Numbers<TNum>.Four * (baseRadiusSq + baseRadius * topRadius + topRadiusSq));
        return Axis.Traverse(t);
    }

    public TNum SlantSurfaceArea => TNum.Pi * (BaseRadius + TopRadius) * SlantHeight;
    /// <inheritdoc />
    public TNum SurfaceArea => SlantSurfaceArea+Top.SurfaceArea+Base.SurfaceArea;

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
            var c=baseIndexer.C;
            var d=baseIndexer.B;
            indices[sideWallBaseIndex] = new(a,c,d);
            indices[sideWallTopIndex] = new(a,b,c);
        }

        return new IndexedMesh<TNum>(vertices, indices);
    }

    private IndexedMesh<TNum> TessellateComplex(int edgeCount)
    {
        var failed = !TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        var baseMesh=new Cone<TNum>(Base.Centroid.LineTo(tip), Base.Radius).Tessellate(edgeCount);
        var topMesh=new Cone<TNum>(Top.Centroid.LineTo(tip), Top.Radius).Tessellate(edgeCount);
        Vector3<TNum>[] vertices = [..baseMesh.Vertices, ..topMesh.Vertices];//acceptable duplication of tip
        var shift=vertices.Length/2;
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

    private Polyline<Vector3<TNum>,TNum> ComplexSweepCurve()
    {
        var failed=!TryGetInflection(out var tip);
        if (failed) throw new InvalidOperationException();
        //both forced same up for same 0 angle result
        var baseRad=TNum.Abs(BaseRadius);
        var topRad=TNum.Abs(TopRadius);
        var normal = Axis.Direction;//gets normalized in ctor
        var start=new Circle3<TNum>(Axis.Start,normal,baseRad).TraverseByAngle(TNum.Zero);
        var end=new Circle3<TNum>(Axis.End,normal,topRad).TraverseByAngle(TNum.Zero);
        return new Polyline<Vector3<TNum>, TNum>(start, tip, end);
    }

    private Line<Vector3<TNum>, TNum> SimpleSweepCurve() 
        => Base.TraverseByAngle(TNum.Zero).LineTo(Top.TraverseByAngle(TNum.Zero));

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis =>Axis.Start.RayThrough(Axis.End);
}