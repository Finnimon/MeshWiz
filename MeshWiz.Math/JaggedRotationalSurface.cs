using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed class JaggedRotationalSurface<TNum> : IReadOnlyList<IRotationalSurface<TNum>>, IRotationalSurface<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Ray3<TNum> Axis;
    public readonly TNum[] Radii;
    public readonly TNum[] EndPositions;
    public TNum Height => EndPositions.Length > 0 ? EndPositions[^1] : TNum.Zero;
    public int Count => EndPositions.Length;
    private Vector3<TNum>? _centroid = null;

    /// <inheritdoc />
    public Vector3<TNum> Centroid => GetCentroid();

    public JaggedRotationalSurface(Ray3<TNum> axis, TNum[] radii, TNum[] endPositions)
    {
        ArgumentOutOfRangeException.ThrowIfEqual(endPositions.Length, 0, nameof(endPositions));
        ArgumentOutOfRangeException.ThrowIfNotEqual(radii.Length, endPositions.Length + 1, nameof(radii));
        Axis = axis;
        Radii = radii;
        EndPositions = endPositions;
    }

    public IRotationalSurface<TNum> this[int index]
        => GetChildSurface(index);

    public IRotationalSurface<TNum> GetChildSurface(int index)
    {
        if (Count <= (uint)index) throw new IndexOutOfRangeException();
        var startRadius = Radii[index];
        var endRadius = Radii[index + 1];
        var start = index == 0 ? TNum.Zero : EndPositions[index - 1];
        var end = EndPositions[index];
        var isNotCircle = start != end;
        IRotationalSurface<TNum> surf = isNotCircle
            ? new ConeSection<TNum>(Axis.LineSection(start, end), startRadius, endRadius)
            : new Circle3Section<TNum>(Axis.Traverse(start), Axis.Direction, startRadius, endRadius);
        return surf;
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
        => Surface.Rotational.Tessellate<JaggedRotationalSurface<TNum>, TNum>(this, tessellationCount);

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve
    {
        get => field ??= CreateSweepCurve(this);
        private init;
    }

    private Polyline<Vector3<TNum>, TNum> CreateSweepCurve(JaggedRotationalSurface<TNum> jaggedRotationalSurface)
    {
        var pCOunt = EndPositions.Length + 1;
        var pts = new Vector3<TNum>[pCOunt];
        var right = new Plane3<TNum>(Axis.Direction, Axis.Origin).Basis.U;

        for (var i = 0; i < this.Radii.Length; i++)
        {
            var radius = Radii[i];
            var alongAxis = i - 1 < 0 ? TNum.Zero : EndPositions[i - 1];
            var onAxis = Axis.Traverse(alongAxis);
            pts[i] = onAxis + right * radius;
        }

        return new Polyline<Vector3<TNum>, TNum>(pts);
    }

    public static JaggedRotationalSurface<TNum> FromSweepCurve(Polyline<Vector3<TNum>, TNum> sweepCurve,
        Ray3<TNum> axis)
    {
        var axisLine = axis.Origin.LineTo(axis.Origin + axis.Direction);
        var endPositions = new TNum[sweepCurve.Count];
        var radii = new TNum[sweepCurve.Points.Length];
        for (var i = 0; i < sweepCurve.Points.Length; i++)
        {
            var p = sweepCurve.Points[i];
            var closest = axisLine.ClosestPoint(p);
            radii[i] = p.DistanceTo(closest);
            if (i == 0)
            {
                axisLine = closest.LineTo(closest + axis.Direction);
                continue;
            }

            var endPosAbs = closest.DistanceTo(axisLine.Start);
            endPositions[i - 1] = endPosAbs;
        }

        return new JaggedRotationalSurface<TNum>(axisLine, radii, endPositions) { SweepCurve = sweepCurve };
    }

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis;

    private Vector3<TNum> GetCentroid()
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