using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CommunityToolkit.Diagnostics;
using MeshWiz.Collections;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>(Ray3<TNum> Axis, Vec2<TNum>[] Positions)
    : IReadOnlyList<RotationalSurface<TNum>.ChildSurface>,
        IRotationalSurface<TNum>,
        IGeodesicProvider<PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    private TNum? _height;
    public TNum Height => _height ??= AABB.From(Positions).Size.Y;
    private Vec3<TNum>? _basisU;
    private Vec3<TNum> BasisU => _basisU ??= new Plane3<TNum>(Axis.Direction, Axis.Origin).Basis.U;
    public int Count => int.Max(Positions.Length - 1, 0);
    private Vec3<TNum>? _centroid;

    /// <inheritdoc />
    public Vec3<TNum> Centroid => _centroid ??= ComputeCentroid();

    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vec3<TNum> Project(Vec2<TNum> p, in Vec3<TNum> u, in Ray3<TNum> axis)
        => axis.Traverse(p.X) + u * p.Y;

    public ChildSurface this[int index]
        => Count > (uint)index ? ChildSurfaces[index] : IndexThrowHelper.Throw<ChildSurface>(index, Count);

    private ChildSurfaceType GetIncompleteChildSurfaceType(int index)
    {
        if (Count < (uint)index)
            IndexThrowHelper.Throw(index, Count);
        var p1 = Positions[index];
        var p2 = Positions[index + 1];
        var delta = p2 - p1;
        var xIsZero = delta.X.IsApproxZero();
        var yIsZero = delta.Y.IsApproxZero();

        return (xIsZero, yIsZero) switch
        {
            (true, true) => ChildSurfaceType.Dead,
            (false, true) => ChildSurfaceType.Cylinder,
            (true, false) => ChildSurfaceType.Circle,
            (false, false) => ChildSurfaceType.Cone
        };
    }

    [field: AllowNull, MaybeNull]
    private ChildSurface[] ChildSurfaces => field ??= Enumerable.Range(0, Count).Select(CreateChildSurface2).ToArray();

    private ChildSurface CreateChildSurface2(int index) =>
        GetIncompleteChildSurfaceType(index) switch
        {
            ChildSurfaceType.Cylinder => CreateCylinder2(index),
            ChildSurfaceType.ConeSection => CreateConical2(index),
            ChildSurfaceType.Cone => CreateConical2(index),
            ChildSurfaceType.Circle => CreateCircular2(index),
            ChildSurfaceType.CircleSection => CreateCircular2(index),
            _ => ChildSurface.CreateDead(index)
        };
    

    private ChildSurface CreateCircular2(int index)
    {
        var (start, end) = Sweep[index];
        var radii = AABB.From(start.Y, end.Y);
        var center = Axis.Traverse(start.X);
        var normal = Axis.Direction;
        if (radii.Min.IsApproxZero())
            return ChildSurface.Create(index, new Circle3<TNum>(center, normal, radii.Max));
        var surf = new Circle3Section<TNum>(center, normal, start.Y, end.Y);
        return ChildSurface.Create(index, surf);
    }



    private ChildSurface CreateConical2(int index)
    {
        var (start, end) = Sweep[index];
        var axisSection = Axis.LineSection(start.X, end.X);
        var isFullCone = start.Y.IsApproxZero() || end.Y.IsApproxZero();
        if (!isFullCone)
        {
            var sec = new ConeSection<TNum>(axisSection, start.Y, end.Y);
            return ChildSurface.Create(index, sec);
        }

        (var radius, axisSection) = start.Y.IsApproxZero() ? (end.Y, axisSection.Reversed()) : (start.Y, axisSection);
        var cone = new Cone<TNum>(axisSection, radius);
        return ChildSurface.Create(index, cone);
    }

   


    private ChildSurface CreateCylinder2(int index)
    {
        var (start, end) = Sweep[index];
        var radius = start.Y;
        var verticalPositions = AABB.From(start.X, end.X);
        var axis = Axis.LineSection(verticalPositions.Min, verticalPositions.Max);
        //var inverted=end.X<start.X;//(todo)
        var surf = new Cylinder<TNum>(axis, radius);
        return ChildSurface.Create(index, surf);
    }

    [field: AllowNull, MaybeNull]
    public Polyline<Vec2<TNum>, TNum> Sweep => field ??= new Polyline<Vec2<TNum>, TNum>(Positions);

    /// <inheritdoc />
    public IEnumerator<ChildSurface> GetEnumerator() => Enumerable.Range(0,Count)
        .Select(i=>ChildSurfaces[i])
        .GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private TNum? _surfaceArea;

    /// <inheritdoc />
    public TNum SurfaceArea => _surfaceArea ??= this.Where(s=>s.Type is not ChildSurfaceType.Dead)
        .Select(s => s.SurfaceArea)
        .Sum();

    private AABB<Vec3<TNum>>? _bbox;

    /// <inheritdoc />
    public AABB<Vec3<TNum>> BBox => _bbox ??= AABB.Combine(this.Select(s => s.BBox));

    /// <inheritdoc />
    public IMesh<TNum> Tessellate()
        => Tessellate(256);

    public IndexedMesh<TNum> Tessellate(int tessellationCount)
        => Surface.Rotational.Tessellate(Positions, Axis, tessellationCount);

    /// <inheritdoc />
    [field: AllowNull, MaybeNull]
    public IDiscreteCurve<Vec3<TNum>, TNum> SweepCurve
    {
        get => field ??= CreateSweepCurve(this);
        private init;
    }

    private static Polyline<Vec3<TNum>, TNum> CreateSweepCurve(RotationalSurface<TNum> surf)
    {
        var u = surf.BasisU;
        var axis = surf.Axis;
        var pts = new Vec3<TNum>[surf.Positions.Length];
        for (var i = 0; i < surf.Positions.Length; i++)
            pts[i] = Project(surf.Positions[i], in u, in axis);
        return new Polyline<Vec3<TNum>, TNum>(pts);
    }


    public static RotationalSurface<TNum> FromSweepCurve(Polyline<Vec3<TNum>, TNum> sweepCurve,
        Ray3<TNum> axis)
    {
        var axisLine = axis.Origin.LineTo(axis.Origin + axis.Direction);
        var positions = new Vec2<TNum>[sweepCurve.Points.Length];
        for (var i = 0; i < sweepCurve.Points.Length; i++)
        {
            var p = sweepCurve.Points[i];
            var closest = axisLine.ClosestPoint(p);
            var radius = p.DistanceTo(closest);
            if (i == 0)
            {
                axisLine = closest.LineTo(closest + axis.Direction);
                positions[0] = new Vec2<TNum>(TNum.Zero, radius);
                continue;
            }

            var absAlong = closest.DistanceTo(axisLine.Start);
            var startToP = p - axisLine.Start;
            var sign = startToP.Dot(axisLine.AxisVector);
            var along = TNum.CopySign(absAlong, sign);
            positions[i] = new Vec2<TNum>(along, radius);
        }

        return new RotationalSurface<TNum>(axisLine, positions);//do not contorted { SweepCurve = sweepCurve };
    }

    /// <inheritdoc />
    public Ray3<TNum> SweepAxis => Axis;

    private Vec3<TNum> ComputeCentroid()
    {
        var total = TNum.Zero;
        var centroid = Vec3<TNum>.Zero;
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
    public Vec3<TNum> NormalAt(Vec3<TNum> p)
    {
        var foundAny = TryFindClosestSurface(p, out var surfIndex);
        return foundAny
            ? this[surfIndex].NormalAt(p)
            : ThrowHelper.ThrowInvalidOperationException<Vec3<TNum>>("No surface found");
    }

    public Vec3<TNum> ClampToSurface(Vec3<TNum> p)
    {
        var foundAny = TryFindClosestSurface(p, out var surfIndex);
        return foundAny
            ? this[surfIndex].ClampToSurface(p)
            : ThrowHelper.ThrowInvalidOperationException<Vec3<TNum>>("No surface found");
    }

    public bool TryFindClosestSurface(Vec3<TNum> p, out int surfaceIndex)
    {
        surfaceIndex = -1;
        if (Count == 0)
            return false;
        if (Count == 1)
        {
            surfaceIndex = 0;
            return true;
        }

        var (closestPos, _) = Axis.LineSection(TNum.Zero, TNum.One).GetClosestPositions(p);
        var radius = Axis.Traverse(closestPos).DistanceTo(p);
        Vec2<TNum> pRelative = new(closestPos, radius);
        var minDist = TNum.PositiveInfinity;
        for (var i = 0; i < Positions.Length - 1; i++)
        {
            var start = Positions[i];
            var end = Positions[i + 1];
            Line<Vec2<TNum>, TNum> line = new(start, end);
            var diff = line.DistanceTo(pRelative);
            if (diff >= minDist)
                continue;
            minDist = diff;
            surfaceIndex = i;
            if (minDist.IsApproxZero())
                return true;
        }

        return surfaceIndex != -1;
    }

    public Line<Vec3<TNum>, TNum> AxisLine => SweepAxis.LineSection(TNum.Zero, Height);
    private AABB<TNum>? _radiusRange;
    public AABB<TNum> RadiusRange => _radiusRange ??= AABB<TNum>.From(this.Positions.Select(p => TNum.Abs(p.Y)));

    public IEnumerable<ChildGeodesic> TraceGeodesics(Vec3<TNum> p,
        Vec3<TNum> dir,
        Func<int, bool> @while)
    {
        if (Count == 0) yield break;

        var found = TryFindClosestSurface(p, out var surfaceIndex);
        if (!found) yield break;

        var previousDir = dir;
        var previousEnd = p;
        var previousNormal = this[surfaceIndex].NormalAt(p);
        RollingList<int> retryOrder = [surfaceIndex];
        var i = -1;
        while (@while(++i))
        {
            var current = ChildGeodesic.CreateDead();
            var surface = ChildSurface.CreateDead();
            while (retryOrder.TryPopFront(out surfaceIndex))
            {
                surface = this[surfaceIndex];
                var newNormal = surface.NormalAt(previousEnd);
                var normalCalcPossible = Vec3<TNum>.IsRealNumber(newNormal)
                                         && Vec3<TNum>.IsRealNumber(previousNormal);
                if (normalCalcPossible && !newNormal.IsParallelTo(previousNormal))
                {
                    var about = previousNormal.Cross(newNormal);
                    var transformAngle = Vec3<TNum>.SignedAngleBetween(previousNormal, newNormal, about);
                    var rotation = Matrix4x4<TNum>.CreateRotation(about, transformAngle);
                    var rotatedDir = rotation.MultiplyDirection(previousDir);
                    previousDir = rotatedDir;
                }

                previousNormal = newNormal;

                var active = Func.Try(surface.GetGeodesicFromEntry, previousEnd, previousDir);
                if(!active)
                    continue;
                var len = active.Value.Length;
                if (len.IsApproxZero() || !TNum.IsFinite(len))
                    continue;

                current = active.Value;
                retryOrder.Clear();
                break;
            }

            if (current.Type is ChildSurfaceType.Dead || surface.Type is ChildSurfaceType.Dead)
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

            var couldBeSameSurfaceAgain = surface.Type is ChildSurfaceType.Cone or ChildSurfaceType.ConeSection;
            if (couldBeSameSurfaceAgain)
                retryOrder.Add(surfaceIndex);

            if (Vec3<TNum>.IsNaN(previousDir))
                yield break;
        }
    }

    


    public IEnumerable<ChildGeodesic> TraceGeodesicsWithChildSurfaces(
        Vec3<TNum> p,
        Vec3<TNum> dir,
        Func<int, bool> @while)
    {
        if (Count == 0) return [];

        var found = TryFindClosestSurface(p, out var surfaceIndex);
        if (!found) return [];

        return ChildGeodesicsInternal(p, dir, @while, surfaceIndex);
    }

    private IEnumerable<ChildGeodesic> ChildGeodesicsInternal(Vec3<TNum> p, Vec3<TNum> dir,
        Func<int, bool> @while, int surfaceIndex)
    {
        var previousDir = dir;
        var previousEnd = p;
        var previousNormal = ChildSurfaces[surfaceIndex].NormalAt(p);
        RollingList<int> retryOrder = [surfaceIndex];
        var i = -1;
        while (@while(++i))
        {
            surfaceIndex = FindNextCurve(retryOrder, previousEnd, previousNormal, previousDir, out var surface,
                out var current);

            if (current.Type is ChildSurfaceType.Dead || surface.Type is ChildSurfaceType.Dead)
                yield break;

            yield return current;

            previousDir = current.ExitDirection;

            if (Vec3<TNum>.IsNaN(previousDir))
                yield break;


            previousEnd = current.End;
            previousNormal = surface.NormalAt(previousEnd);

            FindNextSurface(surfaceIndex, previousEnd, retryOrder, surface.Type);
        }
    }

    private void FindNextSurface(int surfaceIndex, Vec3<TNum> previousEnd, RollingList<int> retryOrder,
        ChildSurfaceType surface)
    {
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

        var couldBeSameSurfaceAgain = surface is ChildSurfaceType.Cone or ChildSurfaceType.ConeSection;
        if (couldBeSameSurfaceAgain)
            retryOrder.Add(surfaceIndex);
    }

    private int FindNextCurve(RollingList<int> retryOrder, Vec3<TNum> previousEnd, Vec3<TNum> previousNormal,
        Vec3<TNum> previousDir,
        out ChildSurface surface, out ChildGeodesic current)
    {
        surface = ChildSurface.CreateDead();
        current = ChildGeodesic.CreateDead();
        int surfaceIndex;
        while (retryOrder.TryPopFront(out surfaceIndex))
        {
            surface = ChildSurfaces[surfaceIndex];
            var newNormal = surface.NormalAt(previousEnd);
            var normalCalcPossible = Vec3<TNum>.IsRealNumber(newNormal)
                                     && Vec3<TNum>.IsRealNumber(previousNormal);
            if (normalCalcPossible && !newNormal.IsParallelTo(previousNormal))
            {
                var about = previousNormal.Cross(newNormal);
                var transformAngle = Vec3<TNum>.SignedAngleBetween(previousNormal, newNormal, about);
                var rotation = Matrix4x4<TNum>.CreateRotation(about, transformAngle);
                var rotatedDir = rotation.MultiplyDirection(previousDir);
                previousDir = rotatedDir;
            }

            previousNormal = newNormal;
            ChildGeodesic iter;
            try
            {
                iter = surface.GetGeodesicFromEntry(previousEnd, previousDir);
            }
            catch
            {
                continue;
            }

            var len = iter.Length;
            if (len.IsApproxZero() || !TNum.IsFinite(len))
                continue;
            current = iter;
            retryOrder.Clear();
            break;
        }

        return surfaceIndex;
    }

    public PeriodicalInfo TracePeriod(Vec3<TNum> p, Vec3<TNum> dir)
    {
        var result = new PeriodicalInfo(new Ray3<TNum>(p, dir), Axis,
            Result<PeriodicalGeodesics, IReadOnlyList<ChildGeodesic>>
                .DefaultFailure);
        if (Count < 1)
            return result;

        List<ChildGeodesic> cache = new(Count * 2);
        var geodesics = TraceGeodesicsWithChildSurfaces(p, dir, _ => true);
        var couldConclude = false;
        var startingSurface = -1;
        var dotSign = 0;
        foreach (var segment in geodesics)
        {
            cache.Add(segment);
            if (startingSurface==-1)
            {
                startingSurface = segment.Index;
                dotSign = segment.ExitDirection.Dot(Axis.Direction).EpsilonTruncatingSign();
                continue;
            }

            if (segment.Index != startingSurface) continue;
            var newDotSign = segment.ExitDirection.Dot(Axis.Direction).EpsilonTruncatingSign();
            var sameDir = newDotSign == dotSign;
            if (!sameDir) continue;
            couldConclude = true;
            break;
        }

        return !couldConclude
            ? result
            : result with { TraceResult = cache };
    }

    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> TraceGeodesicCycles(Vec3<TNum> p, Vec3<TNum> dir,
        int childSurfaceCount)
    {
        var vertices = new List<Vec3<TNum>>();
        var segments = TraceGeodesics(p, dir, i => i < childSurfaceCount)
            .ToArray(); //execute entirely to improve function caching
        if (segments.Length == 0)
            return new();
        if (segments.Length == 1)
            return segments[0].ToPosePolyline();
        var first = Bool.Once();
        return PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>.CreateCulled(
            Polyline.ForceConcat(segments.Select(s => s.ToPosePolyline())));
    }

    /// <inheritdoc />
    PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>
        IGeodesicProvider<PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>, TNum>.GetGeodesic(Vec3<TNum> p1,
            Vec3<TNum> p2)
        => ThrowHelper.ThrowNotSupportedException<PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>();

    /// <inheritdoc />
    public PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> GetGeodesicFromEntry(Vec3<TNum> entryPoint,
        Vec3<TNum> direction)
        => TraceGeodesicCycles(entryPoint, direction, 1000);
}