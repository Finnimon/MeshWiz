using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using MeshWiz.Collections;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Creation
    {
        public static Polyline<TVec, TNum>[] Unify<TVec, TNum>(Queue<Line<TVec, TNum>> segments,
            TNum? squareTolerance = null)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVec : unmanaged, IVec<TVec, TNum>
        {
            var epsilon = squareTolerance ?? TNum.CreateTruncating(0.00001);
            if (segments is { Count: 0 }) return [];
            List<Polyline<TVec, TNum>> polyLines = [];
            LinkedList<Line<TVec, TNum>> connected = [];
            connected.AddLast(segments.Dequeue());
            var checkedSinceLastAdd = 0;
            while (segments.TryDequeue(out var line))
            {
                if (checkedSinceLastAdd > segments.Count)
                {
                    polyLines.Add(Polyline<TVec, TNum>.FromSegments(connected));
                    connected = [];
                    connected.AddLast(line);
                    checkedSinceLastAdd = 0;
                    continue;
                }

                var currentStart = connected.First!.Value.Start;
                var currentEnd = connected.Last!.Value.End;
                var checkedPrev = checkedSinceLastAdd;
                checkedSinceLastAdd = 0;
                if (currentStart.IsApprox(line.End, epsilon))
                {
                    connected.AddFirst(line);
                }
                else if (currentEnd.IsApprox(line.Start, epsilon))
                {
                    connected.AddLast(line);
                }
                else if (currentStart.IsApprox(line.Start, epsilon))
                {
                    connected.AddFirst(line.Reversed());
                }
                else if (currentEnd.IsApprox(line.End, epsilon))
                {
                    connected.AddLast(line.Reversed());
                }
                else
                {
                    segments.Enqueue(line);
                    checkedSinceLastAdd = checkedPrev + 1;
                }
            }

            if (connected.Count > 0)
            {
                polyLines.Add(Polyline<TVec, TNum>.FromSegments(connected));
            }

            return polyLines.ToArray();
        }

        public static Polyline<TVec, TNum>[] UnifyNonReversing<TVec, TNum>(
            RollingList<Line<TVec, TNum>> segments,
            TNum? squareTolerance = null)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVec : unmanaged, IVec<TVec, TNum>
        {
            var epsilon = squareTolerance ?? CalculateMinimumEpsilon(segments);

            if (segments is { Count: 0 }) return [];
            if (segments is { Count: 1 }) return [new Polyline<TVec, TNum>(segments[0].Start, segments[0].End)];

            List<Polyline<TVec, TNum>> polyLines = [];
            RollingList<TVec> connected = new((int)0);
            var checkedSinceLastAdd = int.MaxValue;
            var frontDirection = TVec.NaN;
            var backDirection = TVec.NaN;
            while (segments.TryPopBack(out var line))
            {
                if (line.SquaredLength < epsilon)
                    continue;

                if (checkedSinceLastAdd > segments.Count + 2)
                {
                    AddIfValid(polyLines, connected, epsilon);
                    connected = [line.Start, line.End];
                    checkedSinceLastAdd = 0;
                    backDirection = (frontDirection = line.Direction);
                    continue;
                }

                var connectedStart = connected[0];
                var connectedEnd = connected[^1];
                if (connectedStart.IsApprox(line.End, epsilon))
                {
                    var newFrontDirection = line.Direction;
                    var sameDirection = newFrontDirection.Dot(frontDirection).IsApprox(TNum.One);
                    if (sameDirection) connected[0] = line.Start;
                    else connected.PushFront(line.Start);
                    frontDirection = newFrontDirection;
                    checkedSinceLastAdd = 0;
                }
                else if (connectedEnd.IsApprox(line.Start, epsilon))
                {
                    var newBackDirection = line.Direction;
                    var sameDirection = newBackDirection.Dot(backDirection).IsApprox(TNum.One);
                    if (sameDirection) connected[^1] = line.End;
                    else connected.PushBack(line.End);
                    backDirection = newBackDirection;
                    checkedSinceLastAdd = 0;
                }
                else
                {
                    segments.PushFront(line);
                    checkedSinceLastAdd++;
                }
            }

            AddIfValid(polyLines, connected, epsilon);
            return polyLines.ToArray();
        }

        private static TNum CalculateMinimumEpsilon<TNum, TVec>(IReadOnlyList<Line<TVec, TNum>> segments)
            where TNum : unmanaged, IFloatingPointIeee754<TNum> where TVec : unmanaged, IVec<TVec, TNum>
        {
            var epsilon = TNum.CreateTruncating(float.MaxValue);
            foreach (var segment in segments)
            {
                var sqLength = segment.SquaredLength;
                if (sqLength <= TNum.Epsilon) continue;
                if (sqLength >= epsilon) continue;
                epsilon = sqLength;
            }

            return epsilon / TNum.CreateTruncating(2);
        }


        private static void AddIfValid<TVec, TNum>(List<Polyline<TVec, TNum>> polyLines,
            RollingList<TVec> connected, TNum minLength)
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>

        {
            if (connected.Count < 2) return;

            TryTrimTail<TVec, TNum>(connected);
            
            var poly = new Polyline<TVec, TNum>(connected.ToArray());
            if (poly.Count < 1) return;
            var length = poly.Length;
            if (length < minLength) return;

            polyLines.Add(poly);
        }

        private static void TryTrimTail<TVec, TNum>(RollingList<TVec> connected) where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (connected.Count <= 3 || !connected[0].IsApprox(connected[^1])) return;
            var tailDir=connected[^1]-connected[^2];
            var headDir=connected[0]-connected[1];
            if (!tailDir.IsApprox(headDir)) return;
            connected.PopBack();
            connected[0] = connected.Tail;
        }


        public static Polyline<TVec, TNum>[] UnifyNonReversing<TVec, TNum>(
            RollingList<Polyline<TVec, TNum>> segments,
            TNum? tolerance = null)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVec : unmanaged, IVec<TVec, TNum>
        {
            var epsilon = tolerance ?? TNum.Epsilon;
            if (segments is { Count: 0 }) return [];
            if (segments is { Count: 1 }) return segments.ToArray();

            List<Polyline<TVec, TNum>> polyLines = [];
            RollingList<TVec> connected = new((int)0);
            var checkedSinceLastAdd = int.MaxValue;
            while (segments.TryPopBack(out var segment))
            {
                if (segment.Length < epsilon)
                    continue; //remove
                if (segment.IsClosed)
                {
                    polyLines.Add(segment);
                    continue;
                }

                if (checkedSinceLastAdd > segments.Count + 1 
                    || connected.Count > 3 && connected[0].IsApprox(connected[^1], epsilon))
                {
                    AddIfValid(polyLines, connected, epsilon);
                    connected = [..segment.Points];
                    checkedSinceLastAdd = 0;
                    continue;
                }

                var connectedStart = connected[0];
                var connectedEnd = connected[^1];
                if (connectedStart.IsApprox(segment.End, epsilon))
                {
                    connected.PushFront(segment.Points[..^1]);
                    checkedSinceLastAdd = 0;
                }
                else if (connectedEnd.IsApprox(segment.Start, epsilon))
                {
                    connected.PushBack(segment.Points[1..]);
                    checkedSinceLastAdd = 0;
                }
                else
                {
                    segments.PushFront(segment);
                    checkedSinceLastAdd++;
                }
            }

            AddIfValid(polyLines, connected, epsilon);
            return polyLines.ToArray();
        }


        public static Polyline<TVec, TNum>[] BuildAllConnectedCurves<TVec, TNum>(
            IDictionary<int, RollingList<(int NextPolyline, Polyline<TVec, TNum> seg)>> segments,
            TNum epsilon) where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            List<Polyline<TVec, TNum>> results = [];
            while (segments.Count > 0)
            {
                var start = segments.Keys.First();
                if (!TryBuildConnectedCurve(segments, start, epsilon, out var polyline)) continue;
                results.Add(polyline);
            }

            return results.ToArray();
        }

        public static bool TryBuildConnectedCurve<TVec, TNum>(
            IDictionary<int, RollingList<(int NextPolyline, Polyline<TVec, TNum> seg)>> segments,
            int startingPoint,
            TNum epsilon,
            [NotNullWhen(returnValue: true)] out Polyline<TVec, TNum>? result)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TVec : unmanaged, IVec<TVec, TNum>
        {
            var startingPointFound = segments.TryGetValue(startingPoint, out var startingSegments);
            if (!startingPointFound || startingSegments is not { Count: > 0 })
            {
                result = null;
                return false;
            }

            var startingSeg = startingSegments.PopFront();
            if (startingSegments.Count == 0) segments.Remove(startingPoint);

            if (startingSeg.seg.IsClosed)
            {
                result = startingSeg.seg;
                return true;
            }

            RollingList<TVec> connected = new((ReadOnlySpan<TVec>)startingSeg.seg.Points);
            var nextIndex = startingSeg.NextPolyline;

            while (segments.TryGetValue(nextIndex, out var nextSegs))
            {
                var tested = 0;
                while ((tested++) < nextSegs.Count && nextSegs.TryPopFront(out var nextSeg))
                {
                    var (currentNextIndex, currentSeg) = nextSeg;
                    if (!currentSeg.Start.IsApprox(connected.Tail, epsilon))
                    {
                        nextSegs.PushBack(nextSeg);
                        continue;
                    }

                    nextIndex = currentNextIndex;
                    connected.PushBack(currentSeg.Points[1]);
                    break;
                }

                if (segments.Count == 0) segments.Remove(nextIndex);

                if (!connected.Head.IsApprox(connected.Tail, epsilon)) continue;
                result = new(connected.ToArray());
                return true;
            }

            result = new Polyline<TVec, TNum>(connected.ToArray());
            return true;
        }
    }
}