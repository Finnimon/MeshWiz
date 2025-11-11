using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed record JaggedRotationalSurface<TNum>(Ray3<TNum> Axis, Vector2<TNum>[] Positions)
    : IReadOnlyList<IRotationalSurface<TNum>>,
        IRotationalSurface<TNum>,
        IGeodesicProvider<Polyline<Vector3<TNum>, TNum>, TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
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
    private static Vector3<TNum> Project(Vector2<TNum> p, in Vector3<TNum> u, in Ray3<TNum> axis)
        => axis.Traverse(p.X) + u * p.Y;

    public IRotationalSurface<TNum> this[int index]
        => GetChildSurface(index);

    // public IRotationalSurface<TNum> GetChildSurface(int index)
    // {
    //     if (Count <= (uint)index) IndexThrowHelper.Throw(index, Count);
    //     var start = Positions[index];
    //     var end = Positions[index + 1];
    //     if (start.Y.IsApprox(end.Y))
    //         return new Cylinder<TNum>(Axis.LineSection(start.X, end.X), start.Y);
    //     var isNotCircle = start.X != end.X;
    //     if (isNotCircle)
    //     {
    //         (start, end) = start.Y > end.Y && start.X < end.X ? (start, end) : (end, start);
    //         return new ConeSection<TNum>(Axis.LineSection(start.X, end.X), start.Y, end.Y);
    //     }
    //     else return new Circle3Section<TNum>(Axis.Traverse(start.X), Axis.Direction, start.Y, end.Y);
    // }


    public IRotationalSurface<TNum> GetChildSurface(int index)
    {
        if(index==0)
            Console.WriteLine();
        if (Count <= (uint)index) IndexThrowHelper.Throw(index, Count);
        var start = Positions[index];
        var end = Positions[index + 1];
        var isCircle = start.X.IsApprox(end.X);
        if (isCircle)
            return new Circle3Section<TNum>(Axis.Traverse(start.X), Axis.Direction, start.Y, end.Y);
        var isCylinder = start.Y.IsApprox(end.Y);
        var axisSection = Axis.LineSection(start.X, end.X);
        if (isCylinder)
            return new Cylinder<TNum>(axisSection, TNum.Abs(start.Y));
        var isFullCone = start.Y.IsApproxZero() || end.Y.IsApproxZero();
        if (isFullCone)
        {
            (var radius, axisSection) =
                start.Y.IsApproxZero() ? (end.Y, axisSection.Reversed()) : (start.Y, axisSection);
            return new Cone<TNum>(axisSection, radius);
        }

        return new ConeSection<TNum>(axisSection, start.Y, end.Y);
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
        => Tessellate(256);

    public IndexedMesh<TNum> Tessellate(int tessellationCount)
        => Surface.Rotational.Tessellate(Positions, Axis, tessellationCount);

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IDiscreteCurve<Vector3<TNum>, TNum> SweepCurve
    {
        get => field ??= CreateSweepCurve(this);
        private init;
    }

    private static Polyline<Vector3<TNum>, TNum> CreateSweepCurve(JaggedRotationalSurface<TNum> surf)
    {
        var u = surf.BasisU;
        var axis = surf.Axis;
        var pts = new Vector3<TNum>[surf.Positions.Length];
        for (var i = 0; i < surf.Positions.Length; i++)
            pts[i] = Project(surf.Positions[i], in u, in axis);
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
                positions[0] = new Vector2<TNum>(TNum.Zero, radius);
                continue;
            }

            var absAlong = closest.DistanceTo(axisLine.Start);
            var startToP = p - axisLine.Start;
            var sign = startToP.Dot(axisLine.Direction);
            var along = TNum.CopySign(absAlong, sign);
            positions[i] = new Vector2<TNum>(along, radius);
        }

        return new JaggedRotationalSurface<TNum>(axisLine, positions) { SweepCurve = sweepCurve };
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

    /// <inheritdoc />
    public Vector3<TNum> NormalAt(Vector3<TNum> p)
    {
        var foundAny = TryFindClosestSurface(p, out var surfIndex);
        return foundAny
            ? this[surfIndex].NormalAt(p)
            : ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>("No surface found");
    }

    public Vector3<TNum> ClampToSurface(Vector3<TNum> p)
    {
        var foundAny = TryFindClosestSurface(p, out var surfIndex);
        return foundAny
            ? this[surfIndex].ClampToSurface(p)
            : ThrowHelper.ThrowInvalidOperationException<Vector3<TNum>>("No surface found");
    }

    public bool TryFindClosestSurface(Vector3<TNum> p, out int surfaceIndex)
    {
        surfaceIndex = -1;
        if (Count == 0) return false;
        if (Count == 1)
        {
            surfaceIndex = 0;
            return true;
        }

        var (closestPos, onSegPos) = AxisLine.GetClosestPositions(p);
        var height = Height;
        closestPos *= height;
        // onSegPos *= height;
        var minDist = TNum.PositiveInfinity;
        for (var i = 0; i < Positions.Length - 1; i++)
        {
            var start = Positions[i];
            var end = Positions[i + 1];
            var vBox = AABB.From(start.X, end.X);
            var vDiff = vBox.DistanceTo(closestPos);
            if (vDiff > minDist) continue;
            var childSurf = GetChildSurface(i);
            var diff = childSurf.ClampToSurface(p).DistanceTo(p);
            if (diff >= minDist)
                continue;
            minDist = diff;
            surfaceIndex = i;
        }

        return surfaceIndex != -1;
    }

    public Line<Vector3<TNum>, TNum> AxisLine => SweepAxis.LineSection(TNum.Zero, Height);
    
    public IEnumerable<IContiguousDiscreteCurve<Vector3<TNum>, TNum>> TraceGeodesics(Vector3<TNum> p, Vector3<TNum> dir, Func<int,bool> @while)
    {
        if(Count == 0) yield break;
        
        var found = TryFindClosestSurface(p, out var surfaceIndex);
        if (!found) yield break;
        
        var previousDir = dir;
        var previousEnd = p;
        var previousNormal = this[surfaceIndex].NormalAt(p);
        RollingList<int> retryOrder = [surfaceIndex];
        var i = -1;
        while (@while(++i))
        {
            IContiguousDiscreteCurve<Vector3<TNum>, TNum>? current = null;
            IRotationalSurface<TNum>? surface = null;
            while (retryOrder.TryPopFront(out surfaceIndex))
            {
                surface = this[surfaceIndex];
                var newNormal = surface.NormalAt(previousEnd);
                var normalCalcPossible = Vector3<TNum>.IsRealNumber(newNormal)
                                         && Vector3<TNum>.IsRealNumber(previousNormal);
                if (normalCalcPossible && !newNormal.IsParallelTo(previousNormal))
                {
                    var about = previousNormal.Cross(newNormal);
                    var transformAngle = Vector3<TNum>.SignedAngleBetween(previousNormal, newNormal, about);
                    var rotation = Matrix4x4<TNum>.CreateRotation(about, transformAngle);
                    var rotatedDir = rotation.MultiplyDirection(previousDir);
                    previousDir = rotatedDir;
                }

                previousNormal = newNormal;

                var active = Func.Try(surface.GetGeodesicFromEntry, previousEnd, previousDir);
                var currentTryFailed = !active.HasValue
                                       || active.Value is not IContiguousDiscreteCurve<Vector3<TNum>, TNum> contiguous
                                       || contiguous.Length.IsApproxZero()
                                       || !TNum.IsRealNumber(contiguous.Length);
                if (currentTryFailed)
                {
                    Console.WriteLine(active.Info);
                    continue;
                }

                current = (IContiguousDiscreteCurve<Vector3<TNum>, TNum>)active.Value;
                retryOrder.Clear();
                break;
            }

            if (current is null || surface is null)
                yield break;

            yield return current;

            previousDir = current.ExitDirection;
            previousEnd = current.End;
            previousNormal = surface.NormalAt(previousEnd);

            var minusOneIndex = surfaceIndex != 0 ? surfaceIndex - 1 : Count - 1;
            var minusOneDistance = this[minusOneIndex].ClampToSurface(previousEnd).DistanceTo(previousEnd);
            minusOneDistance = TNum.IsNaN(minusOneDistance) ? TNum.PositiveInfinity : minusOneDistance;

            var plusOneIndex = surfaceIndex != Count - 1 ? surfaceIndex + 1 : 0;
            var plusOneDistance = this[plusOneIndex].ClampToSurface(previousEnd).DistanceTo(previousEnd);
            plusOneDistance = TNum.IsNaN(plusOneDistance) ? TNum.PositiveInfinity : plusOneDistance;

            var (bestDist, bestIndex) = minusOneDistance < plusOneDistance
                ? (minusOneDistance, minusOneIndex)
                : (plusOneDistance, plusOneIndex);
            var nextSurfFound = Numbers<TNum>.Eps3.IsApproxGreaterOrEqual(bestDist);

            retryOrder.Clear();
            if (nextSurfFound)
                retryOrder.Add(bestIndex);

            var couldBeSameSurfaceAgain = surface is Cone<TNum> or ConeSection<TNum>;
            if (couldBeSameSurfaceAgain)
                retryOrder.Add(surfaceIndex);

            if (Vector3<TNum>.IsNaN(previousDir))
                yield break;
        }
    }

    public Result<Arithmetics, Polyline<Vector3<TNum>, TNum>> TraceFullCycle(Vector3<TNum> p, Vector3<TNum> dir)
    {
        List<IContiguousDiscreteCurve<Vector3<TNum>, TNum>> cache = new(Count*2);
        var geodesics = TraceGeodesics(p, dir, _ => true);
        var first = Once.True;
        foreach (var segment in geodesics)
        {
            cache.Add(segment);
            if (first) continue;
            
            
        }

        throw new NotFiniteNumberException();
    }
    public Polyline<Vector3<TNum>, TNum> TraceGeodesicCycles(Vector3<TNum> p, Vector3<TNum> dir, int childSurfaceCount = 100)
    {
        var vertices = new List<Vector3<TNum>>();
        var segments = TraceGeodesics(p, dir,i=>i<childSurfaceCount);
        foreach (var segment in segments)
        {
            var current = segment.ToPolyline();
            var add = vertices.Count == 0 ? current.Points : current.Points[1..];
            vertices.AddRange(add);
        }
        return new Polyline<Vector3<TNum>, TNum>(vertices.ToArray()).CullDeadSegments();
    }

    /// <inheritdoc />
    Polyline<Vector3<TNum>, TNum> IGeodesicProvider<Polyline<Vector3<TNum>, TNum>, TNum>.GetGeodesic(Vector3<TNum> p1,
        Vector3<TNum> p2)
        => ThrowHelper.ThrowNotSupportedException<Polyline<Vector3<TNum>, TNum>>();

    /// <inheritdoc />
    public Polyline<Vector3<TNum>, TNum> GetGeodesicFromEntry(Vector3<TNum> entryPoint, Vector3<TNum> direction)
        => TraceGeodesicCycles(entryPoint, direction, 10000);
}