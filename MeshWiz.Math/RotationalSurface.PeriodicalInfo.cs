using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public sealed partial record RotationalSurface<TNum>
{
    public sealed record PeriodicalInfo(
        Ray3<TNum> StartingConditions,
        Ray3<TNum> Axis,
        Result<PeriodicalGeodesics, IReadOnlyList<ChildGeodesic>> TraceResult
    )
    {
        private Result<PeriodicalGeodesics, Ray3<TNum>>? _exit;
        public Result<PeriodicalGeodesics, Ray3<TNum>> Exit => _exit ??= CalculateExit();

        private Result<PeriodicalGeodesics, Angle<TNum>>? _entryAngle;
        public Result<PeriodicalGeodesics, Angle<TNum>> EntryAngle => _entryAngle ??= CalculateEntryAngle();

        private Ray3<TNum>? _entry;

        public Result<PeriodicalGeodesics, Ray3<TNum>> Entry
        {
            get
            {
                if (!TraceResult)
                    return Result<PeriodicalGeodesics, Ray3<TNum>>.Failure(TraceResult.Info);
                var entryCurve = TraceResult.Value[0];
                return _entry ??= new Ray3<TNum>(entryCurve.Start, entryCurve.EntryDirection);
            }
        }

        private PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>? _exitCurve;

        private Result<PeriodicalGeodesics, Ray3<TNum>> CalculateExit()
        {
            if (_exitCurve is not null)
                return new Ray3<TNum>(_exitCurve.Poses[^1].Origin, _exitCurve.Poses[^1].Front);
            var curves = TraceResult.Value;
            var firstCurve = curves[0];
            Plane3<TNum> startPlane = new(Axis.Direction, firstCurve.Start);
            var lastCurve = curves[^1].ToPosePolyline();
            var intersections = GetIntersectingSegments(startPlane, lastCurve);
            if (intersections.Count < 1)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            var initialEntry = firstCurve.EntryDirection;
            var endingSegment = intersections.OrderBy(i =>
            {
                var dot = lastCurve[i].Direction.Dot(initialEntry);
                var closeness = TNum.Abs(dot - TNum.One);
                return closeness;
            }).First();
            var endingLine = lastCurve[endingSegment];
            var success = startPlane.Intersect(endingLine, out var endingPoint);
            if (!success)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            var dist = Vector3<TNum>.Distance(endingLine.StartPose.Position, endingPoint);
            var distances = lastCurve.CumulativeDistances;
            var totalLength = distances[endingSegment] + dist;
            _exitCurve = lastCurve.ExactSection(TNum.Zero, totalLength);

            return new Ray3<TNum>(endingPoint, endingLine.AxisVector);
        }

        [Pure]
        private static List<int> GetIntersectingSegments(Plane3<TNum> plane,
            PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum> polyline)
        {
            List<int> intersections = [];
            for (var i = 0; i < polyline.Count; i++)
            {
                var doIntersect = plane.DoIntersect(polyline[i]);
                if (!doIntersect) continue;
                intersections.Add(i);
            }

            return intersections;
        }

        private Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>>? _finalizedPath;

        public Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>> FinalizedPath =>
            _finalizedPath ??= FinalizedPolyline();

        private Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>> FinalizedPolyline()
        {
            var sw = Stopwatch.StartNew();
            if (_finalizedPath is not null)
                return _finalizedPath.Value;
            if (_finalizedPoses is { IsSuccess: true })
                return _finalizedPoses.Value.Value.ToPolyline();
            if (!TraceResult || !Exit)
                return Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>>.Failure(!TraceResult
                    ? TraceResult.Info
                    : Exit.Info);

            sw.Restart();
            var segments = TraceResult.Value
                .Take(..^2)
                .Select(c => c.ToPolyline())
                .Append(_exitCurve!.ToPolyline());
            var concat = Polyline.ForceConcat(segments);

            sw.Restart();
            Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>> result;
            result = concat
                ? Polyline<Vector3<TNum>, TNum>.CreateCulled(concat)
                : Result<PeriodicalGeodesics, Polyline<Vector3<TNum>, TNum>>.DefaultFailure;

            sw.Restart();
            return result;
        }

        private Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>? _finalizedPoses;

        public Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>
            FinalizedPoses => _finalizedPoses ??= FinalizePoses();

        private Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>> FinalizePoses()
        {
            if (!TraceResult || !Exit)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>.Failure(!TraceResult
                    ? TraceResult.Info
                    : Exit.Info);
            var segments = TraceResult.Value
                .Take(..^2)
                .Select(c => c.ToPosePolyline())
                .Append(_exitCurve!);
            var poses = Polyline.ForceConcat(segments);
            if (!poses)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>.DefaultFailure;
            return PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>.CreateCulled(poses);
        }

        private Result<PeriodicalGeodesics, Angle<TNum>>? _phase;
        public Result<PeriodicalGeodesics, Angle<TNum>> Phase => _phase ??= GetPhase();

        private Result<PeriodicalGeodesics, Angle<TNum>> GetPhase()
        {
            var entry = Entry;
            var exit = Exit;
            if (!entry || !exit)
                return Result<PeriodicalGeodesics, Angle<TNum>>.Failure(exit.Info);
            return AngleAbout(entry.Value.Origin, exit.Value.Origin, Axis);
        }

        [Pure]
        private static Angle<TNum> AngleAbout(Vector3<TNum> p1, Vector3<TNum> p2, in Ray3<TNum> axis)
        {
            var v1 = p1 - axis.Origin;
            var v2 = p2 - axis.Origin;
            return Vector3<TNum>.SignedAngleBetween(v1, v2, axis.Direction);
        }

        public Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>> CreatePattern(
            int patternCount, bool useParallel = false)
        {
            if (patternCount <= 0)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>.DefaultFailure;
            var finalized = FinalizedPoses;
            if (!finalized)
                return finalized;
            var source = finalized.Value;
            if (source.Count == 0 || patternCount == 1)
                return finalized;

            var phase = this.Phase;
            if (!phase)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>>.Failure(phase.Info);
            var segments = new PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>[patternCount];
            segments[0] = source;
            if (useParallel)
                Parallel.For(1, patternCount,
                    i => segments[i] = Rotate(source, phase.Value * TNum.CreateTruncating(i), Axis));
            else
                for (var i = 1; i < patternCount; i++)
                    segments[i] = Rotate(source, phase.Value * TNum.CreateTruncating(i), Axis);
            return new PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>(Polyline.ForceConcat(segments));
        }

        private static PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum> Rotate(
            PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum> source, TNum angle, Ray3<TNum> axis)
        {
            var poses = source.Poses.ToArray();
            var rot = Matrix4x4<TNum>.CreateRotation(axis.Direction, angle);
            for (var i = 0; i < poses.Length; i++)
            {
                var pose = poses[i];
                var origin = pose.Origin - axis.Origin;
                origin = rot.MultiplyDirection(origin) + axis.Origin;
                var front = rot.MultiplyDirection(pose.Front);
                var up = rot.MultiplyDirection(pose.Up);
                poses[i] = Pose3<TNum>.CreateUnsafe(origin, front, up);
            }

            return PosePolyline<Pose3<TNum>, Vector3<TNum>, TNum>.CreateNonCopying(poses);
        }

        /// <param name="width">the absolute width, not when considering the angle</param>
        /// <returns></returns>
        public Result<PeriodicalGeodesics, TNum> CalculateOverlap(TNum width, int maxTries = 10000)
        {
            if (width < TNum.Zero)
                return Result<PeriodicalGeodesics, TNum>.DefaultFailure;
            if (width.IsApproxZero())
                return TNum.Zero;
            if (!Phase)
                return Result<PeriodicalGeodesics, TNum>.Failure(Phase.Info);
            TNum phase = Phase.Value;
            if (!EntryAngle)
                return Result<PeriodicalGeodesics, TNum>.Failure(EntryAngle.Info);
            TNum entryAngle = EntryAngle.Value;

            var realWidth = GetRealWidth(width, entryAngle);
            var radius = Axis.DistanceTo(Entry.Value.Origin);
            var circ = Numbers<TNum>.TwoPi * radius;
            var fractionWidth = realWidth / circ;
            var angularWidth = fractionWidth * Numbers<TNum>.TwoPi;
            var angleHitBox = AABB.Around(TNum.Zero, Numbers<TNum>.Two * angularWidth);
            var currentPhase = phase.Wrap(-TNum.Pi, TNum.Pi);
            while (!angleHitBox.Contains(currentPhase) && --maxTries >= 0)
            {
                currentPhase += phase;
                currentPhase = currentPhase.Wrap(-TNum.Pi, TNum.Pi);
            }

            if (maxTries < 0)
                return Result<PeriodicalGeodesics, TNum>.Failure(PeriodicalGeodesics.CyclesExceeded);
            var currentDistance = TNum.Abs(currentPhase) / Numbers<TNum>.TwoPi;
            var entryTapeBox = AABB.Around(TNum.Zero, realWidth);
            var overlapping = AABB.Around(currentDistance, realWidth);
            var overlap = entryTapeBox & overlapping;
            var absoluteOverlap = TNum.Max(TNum.Zero, overlap.Size);
            return absoluteOverlap / realWidth;
        }


        [Pure]
        private static TNum GetRealWidth(TNum width, TNum entryAngle)
        {
            var realWidth = TNum.Cos(entryAngle) * width;
            return realWidth;
        }

        private Result<PeriodicalGeodesics, Angle<TNum>> CalculateEntryAngle()
        {
            if (!Entry)
                return Result<PeriodicalGeodesics, Angle<TNum>>.Failure(Entry.Info);
            Ray3<TNum> entry = Entry;
            var entryDir = entry.Direction;
            var entryP = entry.Origin;
            Line<Vector3<TNum>, TNum> axisLine = Axis;
            var closest = axisLine.ClosestPoint(entryP);
            var about = entryP - closest;
            Angle<TNum> result = Vector3<TNum>.SignedAngleBetween(Axis.Direction, entryDir, about);
            return result;
        }

        public Result<PeriodicalGeodesics, TNum> CalculateCoverage(TNum width) => throw new NotImplementedException();
    }
}