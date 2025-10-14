using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record JaggedRotationalSurface<TNum>(Ray3<TNum> Axis, Vector2<TNum>[] Positions)
    : IReadOnlyList<IRotationalSurface<TNum>>, IRotationalSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private TNum? _height;
    public TNum Height => _height ??= AABB.From(Positions).Size.Y;
    private Vector3<TNum>? _basisU;
    private Vector3<TNum> BasisU => _basisU ??= new Plane3<TNum>(Axis.Direction, Axis.Origin).Basis.U;
    public int Count => int.Max(Positions.Length - 1, 0);
    private Vector3<TNum>? _centroid;
    /// <inheritdoc />
    public Vector3<TNum> Centroid => _centroid ??= ComputeCentroid();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3<TNum> Project(Vector2<TNum> p,in Vector3<TNum> u, in Ray3<TNum> axis) 
        => axis.Traverse(p.X) + u * p.Y;

    public IRotationalSurface<TNum> this[int index]
        => GetChildSurface(index);

    public IRotationalSurface<TNum> GetChildSurface(int index)
    {
        if (Count <= (uint)index) throw new IndexOutOfRangeException();
        var start = Positions[index];
        var end=Positions[index+1];
        var isNotCircle = start.X != end.X;
        return isNotCircle
            ? new ConeSection<TNum>(Axis.LineSection(start.X, end.X), start.Y, end.Y)
            : new Circle3Section<TNum>(Axis.Traverse(start.X), Axis.Direction, start.Y, end.Y);
    }


    /// <inheritdoc />
    public IEnumerator<IRotationalSurface<TNum>> GetEnumerator()
        => Enumerable.Range(0, Count).Select(GetChildSurface).GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private TNum? _surfaceArea;

    /// <inheritdoc />
    public TNum SurfaceArea => _surfaceArea ??= this.Select(s => s.SurfaceArea).Sum();

    private AABB<Vector3<TNum>>? _bbox;

    /// <inheritdoc />
    public AABB<Vector3<TNum>> BBox => _bbox ??= AABB.Combine(this.Select(s => s.BBox));

    /// <inheritdoc />
    public IMesh<TNum> Tessellate()
        => Tessellate(32);

    public IndexedMesh<TNum> Tessellate(int tessellationCount)
        => Surface.Rotational.Tessellate<JaggedRotationalSurface<TNum>, TNum>(this, tessellationCount,true);

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve
    {
        get => field ??= CreateSweepCurve(this);
        private init;
    }

    private Polyline<Vector3<TNum>, TNum> CreateSweepCurve(JaggedRotationalSurface<TNum> jaggedRotationalSurface)
    {
        var u = BasisU;
        var axis = Axis;
        var pts=new Vector3<TNum>[Positions.Length];
        for (var i = 0; i < Positions.Length; i++)
            pts[i]=Project(Positions[i],in u,in axis);
        return new Polyline<Vector3<TNum>, TNum>(pts);
    }   


    public static JaggedRotationalSurface<TNum> FromSweepCurve(Polyline<Vector3<TNum>, TNum> sweepCurve,
        Ray3<TNum> axis)
    {
        var axisLine = axis.Origin.LineTo(axis.Origin + axis.Direction);
        var positions = new Vector2<TNum>[sweepCurve.Points.Length];
        for (var i = 0; i < sweepCurve.Points.Length; i++)
        {
            var p = sweepCurve.Points[i];
            var closest = axisLine.ClosestPoint(p);
            var radius = p.DistanceTo(closest);
            if (i == 0)
            {
                axisLine = closest.LineTo(closest + axis.Direction);
                positions[0]=new Vector2<TNum>(TNum.Zero,radius);
                continue;
            }

            var absAlong = closest.DistanceTo(axisLine.Start);
            var startToP = p - axisLine.Start;
            var sign = startToP.Dot(axisLine.Direction);
            var along = TNum.CopySign(absAlong, sign);
            positions[i]=new Vector2<TNum>(along,radius);
        }

        return new JaggedRotationalSurface<TNum>(axisLine, positions)
        { SweepCurve = sweepCurve };
    }

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis;

    private Vector3<TNum> ComputeCentroid()
    {
        var total = TNum.Zero;
        var centroid = Vector3<TNum>.Zero;
        foreach (var rotationalSurface in this)
        {
            var area = rotationalSurface.SurfaceArea;
            area = TNum.Abs(area);
            total += area;
            centroid += rotationalSurface.Centroid * area;
        }

        _surfaceArea ??= total;
        return centroid / total;
    }
}