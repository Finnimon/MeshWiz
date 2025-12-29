using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using MeshWiz.Collections;
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
            if (!TraceResult)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.Failure(TraceResult.Info);
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

        public Result<PeriodicalGeodesics,(TNum Overlap,int Pattern)> CalculateOverlap(TNum width, int maxTries = 10_000)
        {
            if (!Phase.TryGetValue(out var phase))
                return Phase.Info;
            if (!EntryAngle.TryGetValue(out var entryAngle))
                return EntryAngle.Info;
            if (!Radius.TryGetValue(out var radius))
                return Radius.Info;
            if (width.IsApproxZero())
                return (TNum.Zero,0);
            var realWidth = GetRealWidth(width, entryAngle);
            var relWidth = realWidth / (Numbers<TNum>.TwoPi * radius);
            var phaseStep = (phase / Numbers<TNum>.TwoPi).WrapSaturating();
            var widthBox = AABB.Around(TNum.Zero, relWidth);
            var curStep = 0;
            var half = Numbers<TNum>.Half;
            while (++curStep < maxTries)
            {
                var d = TNum.Abs((phaseStep * TNum.CreateTruncating(curStep)).Wrap(-half,half));
                if(!relWidth.IsApproxGreaterOrEqual(d))
                    continue;
                var absOverlap = relWidth - (d-relWidth*half);

                var overlap = absOverlap/relWidth;
                var pattern = ExpectedFullCoveragePattern(overlap,relWidth);
                if(pattern<0)
                    Console.WriteLine($"{curStep} {pattern} {overlap}");
                return (overlap,pattern);
            }
            return Result<PeriodicalGeodesics, (TNum Overlap, int Pattern)>.DefaultFailure;
        }

        private static int ExpectedFullCoveragePattern(TNum overlap, TNum relWidth)
        {
            var realRelWidth = relWidth*(TNum.One - overlap);
            return int.CreateTruncating(TNum.Ceiling(TNum.One / realRelWidth))/2;
        }
        public Result<PeriodicalGeodesics, TNum> Radius => Entry.Select(e => e.Origin).Select(Axis.DistanceTo);
        public Result<PeriodicalGeodesics, (TNum Covereage, TNum Overlap, int Pattern)>
            CalculateCoverageAndOverlap(TNum width, TNum thickness = default, int pattern = -1)
        {
            if (!Phase.TryGetValue(out var phase))
                return Phase.Info;
            if (thickness == default) thickness = TNum.One;
            var radius = Axis.DistanceTo(Entry.Value.Origin);
            var circ = Numbers<TNum>.TwoPi * radius;
            var relWidth = width / circ;
            var res = int.CreateTruncating(TNum.One / relWidth) * 32;
            var mapping = CreateThicknessMapping(res,
                phase,
                relWidth,
                pattern == -1,
                ref pattern,
                thickness);
            var coveredPixels = mapping.Where(v => v > thickness).ToArray();
            var coverage = -TNum.CreateTruncating(mapping.Length-coveredPixels.Length) 
                           / TNum.CreateTruncating(mapping.Length);
            coverage += TNum.One;
            Console.WriteLine(++ovl);
            var overlap = Numbers<TNum>.AverageOf(coveredPixels) / thickness-TNum.One;
            return (coverage, overlap, pattern);
        }

        private static int ovl = 0;

        private static TNum[] CreateThicknessMapping(int resolution,
            TNum phase,
            TNum relWidth,
            bool computeCoveringPattern,
            ref int pattern,
            TNum thickness = default)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(resolution, 0, nameof(phase));
            if (thickness == default)
                thickness = Numbers<TNum>.Eps4;
            var relShift = phase / Numbers<TNum>.TwoPi;
            var mappingArr = new TNum[resolution];
            var mappingSpan = mappingArr.AsSpan();
            RollingSpan<TNum> mapping = mappingSpan;

            var curPattern = 0;
            DrawThicknessPosition(relWidth, mapping, TNum.Zero, thickness);
            DrawThicknessPosition(relWidth, mapping, relShift, thickness);
            curPattern++;
            while (curPattern++ < pattern
                   || computeCoveringPattern && mappingSpan.Contains(TNum.Zero))
            {
                var pos = TNum.CreateTruncating(curPattern) * relShift;
                var wrappedAround = pos.IsApproxZero();
                if(wrappedAround)
                {
                    curPattern--;
                    break;
                }
                DrawThicknessPosition(relWidth,
                    mapping,
                    pos,
                    thickness);
            }
            pattern = curPattern;
            return mappingArr;
        }

        private static void DrawThicknessPosition(TNum relWidth,
            RollingSpan<TNum> mapping,
            TNum pos,
            TNum thickness)
        {
            pos = pos.WrapSaturating();
            var lengthNum = TNum.CreateTruncating(mapping.Length);
            var width = relWidth * lengthNum;
            var pixels = int.CreateTruncating(TNum.Ceiling(width));
            var targetPixel = pos * lengthNum;
            var subPixelShift = targetPixel.WrapSaturating();
            var targetPixelIndex = int.CreateTruncating(TNum.Floor(targetPixel));
            var postPixelSmoothed = thickness * subPixelShift;
            var prePixelSmoothed = TNum.One - postPixelSmoothed;
            var firstPixel = targetPixelIndex - pixels / 2;
            mapping[firstPixel - 1] += prePixelSmoothed;

            for (var i = 0; i < pixels; i++)
                mapping[i + firstPixel] += thickness;
            mapping[firstPixel + pixels + 1] += postPixelSmoothed;
        }


        [Pure]
        private TNum GetRealWidth(TNum width,Angle<TNum> entry) => TNum.Abs(width / TNum.Cos(entry));

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

        public Result<PeriodicalGeodesics, Ray3<TNum>> Exit2()
        {
            if (!TraceResult)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.Failure(TraceResult.Info);
            var trace = TraceResult.Value;
            if (trace.Count < 1)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            var firstCurve = trace[0];
            var lastCurve = trace[^1];
            Plane3<TNum> startPlane = new(Axis.Direction, firstCurve.Start);

            var param = lastCurve.SolveIntersection(startPlane);

            if (!param)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            return lastCurve.GetRay(param);
        }

        public Result<PeriodicalGeodesics, Ray3<TNum>> Exit3()
        {
            if (!TraceResult)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.Failure(TraceResult.Info);
            var trace = TraceResult.Value;
            if (trace.Count < 1)
                return Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
            var firstCurve = trace[0];
            var lastCurve = trace[^1];
            Plane3<TNum> startPlane = new(Axis.Direction, firstCurve.Start);

            var solve = Curve.Solver.IntersectionBinary(lastCurve, startPlane)
                .Select(lastCurve.GetPose)
                .Select(Pose3<TNum>.FrontRay);

            return solve ? solve.Value : Result<PeriodicalGeodesics, Ray3<TNum>>.DefaultFailure;
        }
    }
}