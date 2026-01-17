using System.Diagnostics.Contracts;
using MeshWiz.Utility;

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
        public Result<PeriodicalGeodesics, Ray3<TNum>> Exit => _exit ??= CreateExit();
        private TNum _exitParameter;

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


        private Result<PeriodicalGeodesics, Polyline<Vec3<TNum>, TNum>>? _finalizedPath;

        public Result<PeriodicalGeodesics, Polyline<Vec3<TNum>, TNum>> FinalizedPath =>
            _finalizedPath ??= FinalizedPolyline();

        private Result<PeriodicalGeodesics, Polyline<Vec3<TNum>, TNum>> FinalizedPolyline()
        {
            if (_finalizedPath is not null)
                return _finalizedPath.Value;
            if (_finalizedPoses.TryGetValue(out var posesResult)
                && posesResult.TryGetValue(out var poses))
                return poses.ToPolyline();
            if (!TraceResult.TryGetValue(out var trace))
                return TraceResult.Info;
            if (!Exit)
                return Exit.Info;

            var last = trace.Count - 1;
            var segments = trace
                .Select((c, i) =>
                    i != last
                        ? c.ToPolyline()
                        : c.Section(TNum.Zero, _exitParameter).ToPolyline());
            var concat = Polyline.ForceConcat(segments);

            return concat
                ? Polyline<Vec3<TNum>, TNum>.CreateCulled(concat)
                : Result<PeriodicalGeodesics, Polyline<Vec3<TNum>, TNum>>.DefaultFailure;
        }

        private Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>? _finalizedPoses;

        public Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>
            FinalizedPoses => _finalizedPoses ??= FinalizePoses();

        private Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>> FinalizePoses()
        {
            if (!TraceResult.TryGetValue(out var trace))
                return TraceResult.Info;
            if (!Exit)
                return Exit.Info;

            var last = trace.Count - 1;
            var segments = trace
                .Select((c, i) =>
                    i != last
                        ? c.ToPosePolyline()
                        : c.Section(TNum.Zero, _exitParameter).ToPosePolyline());
            var poses = Polyline.ForceConcat(segments);
            return !poses
                ? Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>.DefaultFailure
                : PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>.CreateCulledNonCopying(poses);
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
        private static Angle<TNum> AngleAbout(Vec3<TNum> p1, Vec3<TNum> p2, in Ray3<TNum> axis)
        {
            var v1 = p1 - axis.Origin;
            var v2 = p2 - axis.Origin;
            return Vec3<TNum>.SignedAngleBetween(v1, v2, axis.Direction);
        }

        public Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>> CreatePattern(
            int patternCount, bool useParallel = false)
        {
            if (patternCount <= 0)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>.DefaultFailure;
            var finalized = FinalizedPoses;
            if (!finalized)
                return finalized;
            var source = finalized.Value;
            if (source.Count == 0 || patternCount == 1)
                return finalized;

            var phase = this.Phase;
            if (!phase)
                return Result<PeriodicalGeodesics, PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>>.Failure(phase.Info);
            var segments = new PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>[patternCount];
            segments[0] = source;
            if (useParallel)
                Parallel.For(1, patternCount,
                    i => segments[i] = Rotate(source, phase.Value * TNum.CreateTruncating(i), Axis));
            else
                for (var i = 1; i < patternCount; i++)
                    segments[i] = Rotate(source, phase.Value * TNum.CreateTruncating(i), Axis);
            return new PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>(Polyline.ForceConcat(segments));
        }

        private static PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> Rotate(
            PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum> source, TNum angle, Ray3<TNum> axis)
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

            return PosePolyline<Pose3<TNum>, Vec3<TNum>, TNum>.CreateNonCopying(poses);
        }

        public Result<PeriodicalGeodesics, (TNum Overlap, int Pattern)> CalculateOverlap(TNum width,
            int maxTries = 10_000)
        {
            return Result<PeriodicalGeodesics, (TNum Overlap, int Pattern)>.DefaultFailure; //todo
        }

        public Result<PeriodicalGeodesics, TNum> Radius => Entry.Select(e => e.Origin).Select(Axis.DistanceTo);


        [Pure]
        private static TNum CalculateCircumferentialWidth(TNum width, Angle<TNum> entry) => TNum.Abs(width / TNum.Cos(entry));

        private Result<PeriodicalGeodesics, Angle<TNum>> CalculateEntryAngle()
        {
            if (!Entry)
                return Result<PeriodicalGeodesics, Angle<TNum>>.Failure(Entry.Info);
            Ray3<TNum> entry = Entry;
            var entryDir = entry.Direction;
            var entryP = entry.Origin;
            Line<Vec3<TNum>, TNum> axisLine = Axis;
            var closest = axisLine.ClosestPoint(entryP);
            var about = entryP - closest;
            Angle<TNum> result = Vec3<TNum>.SignedAngleBetween(Axis.Direction, entryDir, about);
            return result;
        }

        public Result<PeriodicalGeodesics, Ray3<TNum>> CreateExit()
        {
            if (!TraceResult)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.Failure(TraceResult.Info);
            var trace = TraceResult.Value;
            if (trace.Count < 1)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            var firstCurve = trace[0];
            var lastCurve = trace[^1];
            Plane<TNum> startPlane = new(Axis.Direction, firstCurve.Start);

            var param = lastCurve.SolveIntersection(startPlane);

            if (!param)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            _exitParameter = param;
            return lastCurve.GetRay(param);
        }
    }
}