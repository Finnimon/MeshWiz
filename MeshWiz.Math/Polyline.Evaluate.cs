using System.Diagnostics.Contracts;
using System.Numerics;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Evaluate
    {
        public static TNum SignedArea<TNum>(Polyline<Vector2<TNum>, TNum> polyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (!polyline.IsClosed) return TNum.Zero;

            var points = polyline.Points;
            var prev = points[0];
            var area = TNum.Zero;
            for (var i = 1; i < points.Length; i++)
            {
                var next = points[i];
                area += prev.Cross(next);
                prev = next;
            }

            return area / TNum.CreateTruncating(2);
        }

        public static TNum Area<TNum>(Polyline<Vector2<TNum>, TNum> polyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            => TNum.Abs(SignedArea(polyline));

        public static int AreaSign<TNum>(Polyline<Vector2<TNum>, TNum> polyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            => SignedArea(polyline).EpsilonTruncatingSign();


        public static WindingOrder GetWindingOrder<TNum>(Polyline<Vector2<TNum>, TNum> polyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (!polyline.IsClosed) return WindingOrder.NotClosed;

            var minX = TNum.PositiveInfinity;
            var index = -1;
            for (var i = 0; i < polyline.Points.Length; i++)
            {
                var p = polyline.Points[i];
                if (p.X >= minX) continue;
                minX = p.X;
                index = i;
            }

            var prevIndex = index == 0 ? polyline.Points.Length - 2 : index - 1;
            var nextIndex = index == polyline.Points.Length - 1 ? 1 : index + 1;
            var extreme = polyline.Points[index];
            var u = extreme - polyline.Points[prevIndex];
            var v = polyline.Points[nextIndex] - extreme;
            if (u.IsParallelTo(v))
                return u.Y < TNum.Zero
                    ? WindingOrder.CounterClockwise
                    : WindingOrder.Clockwise;

            var sign = u.CrossSign(v);
            return sign switch
            {
                -1 => WindingOrder.Clockwise,
                0 => u.Y < TNum.Zero ? WindingOrder.CounterClockwise : WindingOrder.Clockwise,
                1 => WindingOrder.CounterClockwise,
                _ => throw new InvalidOperationException(nameof(Vector2<>.CrossSign))
            };
        }


        [Pure]
        public static bool IsConvex<TNum>(Polyline<Vector2<TNum>, TNum> closedPolyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (!closedPolyline.IsClosed)
                throw new ArgumentException("Polyline must be closed", nameof(closedPolyline));

            var prevSign = 0;
            var prevDirection = closedPolyline[0].Direction;
            for (var i = 1; i < closedPolyline.Count; i++)
            {
                var curDirection = closedPolyline[i].Direction;
                var crossSign = prevDirection.CrossSign(curDirection);
                if (prevSign == 0) prevSign = crossSign;

                prevDirection = curDirection;
                if (crossSign == 0) continue; //parallel lines are allowable
                if (crossSign != prevSign) return false;
            }

            return true;
        }
    }

    public static bool DoIntersect<TNum>(Polyline<Vector2<TNum>, TNum> polyline, Polyline<Vector2<TNum>, TNum> other)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (!polyline.BBox.IntersectsWith(other.BBox)) return false;
        return polyline.Any(line => other.Any(otherLine => Line.TryIntersectOnSegment(line, otherLine, out _)));
    }

    public static SortedDictionary<int, List<(int With, TNum at)>> FindSelfIntersections<TNum>(
        Polyline<Vector2<TNum>, TNum> polygon)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        SortedDictionary<int, List<(int With, TNum at)>> intersections = [];
        for (var a = 0; a < polygon.Count; a++)
        {
            var lineA = polygon[a];
            //ignore end
            for (var b = a + 2; b < polygon.Count; b++)
            {
                var lineB = polygon[b];
                if (!Line.TryIntersectOnSegment(lineA, lineB, out var alongA, out var alongB)) continue;

                if (!intersections.TryGetValue(a, out var container))
                {
                    container = [];
                    intersections.Add(a, container);
                }

                container.Add((b, alongA));

                if (!intersections.TryGetValue(b, out container))
                {
                    container = [];
                    intersections.Add(b, container);
                }

                container.Add((a, alongB));
            }
        }

        return intersections;
    }

    public static Polyline<Vector2<TNum>, TNum>[] DegenerateIntersections<TNum>(
        Polyline<Vector2<TNum>, TNum> polygon,
        SortedDictionary<int, List<(int With, TNum at)>>? intersectionLookup = null)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        intersectionLookup ??= FindSelfIntersections(polygon);
        if (intersectionLookup.Count < 2)
        {
            polygon.Points[^1] = polygon.Points[0];
            return [polygon];
        }

        if (intersectionLookup.Count == 2)
        {
            var first = intersectionLookup.First();
            var last = intersectionLookup.Last();
            if (first.Key == 0 && last.Key == polygon.Count - 1)
            {
                polygon.Points[^1] = polygon.Points[0];
                return [polygon];
            }

            var start = polygon.CumulativeDistances[first.Key] + first.Value[0].at;
            var end = polygon.CumulativeDistances[last.Key] + last.Value[0].at;
            return [polygon.ExactSection(start, end)];
        }

        intersectionLookup.Values.ForEach(v
            => v.Sort((ints, ints1) => ints.at.CompareTo(ints1.at)));

        List<Polyline<Vector2<TNum>, TNum>> segs = [];
        RollingList<Vector2<TNum>> active = [];
        var keys = intersectionLookup.Keys.ToArray();
        for (var i = 0; i < keys.Length; i++)
        {
            var curKey = keys[i];
            var nextKey = i + 1 < keys.Length ? keys[i + 1] : keys[0];
            var cur = intersectionLookup[curKey];
            TNum start;
            TNum end;
            for (var j = 0; j < cur.Count - 1; j++)
            {
                start = polygon.CumulativeDistances[curKey] + cur[j].at;
                end = polygon.CumulativeDistances[curKey] + cur[j + 1].at;
                polygon.ExactSection(start, end);
            }

            start = polygon.CumulativeDistances[curKey] + cur[^1].at;
            end = polygon.CumulativeDistances[curKey] + intersectionLookup[nextKey][0].at;
            segs.Add(polygon.ExactSection(start, end));
        }

        List<Polyline<Vector2<TNum>, TNum>> results = [];
        var alive = new bool[segs.Count];
        Array.Fill(alive, true);
        for (var i = 0; i < segs.Count; i++)
        {
            if (!alive[i]) continue;
            alive[i] = false;
            var seg = segs[i];
            for (var j = i + 2; i < segs.Count + 2; j++)
            {
                var jIndex = j < segs.Count ? j : j - segs.Count;
                if (!alive[jIndex]) continue;
                var otherSeg = segs[j];
            }
        }

        throw new NotImplementedException();
    }


    public static RollingList<Polyline<Vector2<TNum>, TNum>> FindSegments<TNum>(
        Polyline<Vector2<TNum>, TNum> polygon,
        TNum minSegLength)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var level = Simplicity.MultiCheck(polygon);
        if (level == Simplicity.Level.Simple)
            return [polygon];
        List<TNum> ranges = [];

        for (var aIndex = 0; aIndex < polygon.Count; aIndex++)
        {
            var a = polygon[aIndex];
            var cumDistance = polygon.CumulativeDistances[aIndex];
            for (var bIndex = aIndex + 2; bIndex < polygon.Count; bIndex++)
            {
                var b = polygon[bIndex];
                var doIntersect = Line.TryIntersectOnSegment(a, b, out var alongA, out var alongB);
                if (!doIntersect) continue;
                ranges.Add(cumDistance + alongA * a.Length);
                ranges.Add(polygon.CumulativeDistances[bIndex] + alongB * b.Length);
            }
        }

        ranges.Sort();
        RollingList<Polyline<Vector2<TNum>, TNum>> segments = new(ranges.Count);

        for (var i = 0; i < ranges.Count - 1; i++)
        {
            var rangeStart = ranges[i];
            var rangeEnd = ranges[i + 1];
            var rangeLen = rangeEnd - rangeStart;
            if (rangeLen < minSegLength) continue;
            segments.Add(polygon.ExactSection(rangeStart, rangeEnd));
        }

        if (polygon.IsClosed && !ranges[0].IsApprox(TNum.Zero)) segments.Add(polygon.ExactSection(ranges[^1], ranges[0]));
        return segments;
    }

    public static RollingList<(TNum start, TNum end, TNum next)> FindIdentifiableSegments<TNum>(
        Polyline<Vector2<TNum>, TNum> polygon,
        TNum minSegLength)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var level = Simplicity.MultiCheck(polygon);
        if (level == Simplicity.Level.Simple)
            return [(TNum.Zero, polygon.Length, TNum.Zero)];
        List<(TNum end, TNum next)> ranges = [];

        for (var aIndex = 0; aIndex < polygon.Count; aIndex++)
        {
            var a = polygon[aIndex];
            var cumDistance = polygon.CumulativeDistances[aIndex];
            for (var bIndex = aIndex + 2; bIndex < polygon.Count; bIndex++)
            {
                var b = polygon[bIndex];
                var doIntersect = Line.TryIntersectOnSegment(a, b, out var alongA, out var alongB);
                if (!doIntersect) continue;
                var aPos = cumDistance + alongA * a.Length;
                var bPos = polygon.CumulativeDistances[bIndex] + alongB * b.Length;
                ranges.Add((aPos,bPos));
                ranges.Add((bPos,aPos));
            }
        }

        ranges.Sort((r1,r2)=>r1.end.CompareTo(r2.end));
        
        RollingList<(TNum start, TNum end, TNum next)> segments = new(ranges.Count);
        for (var i = 0; i < ranges.Count - 1; i++)
        {
            var start = ranges[i].end;
            var range = ranges[i + 1];
            var end = range.end;
            var next = range.next;
            var rangeLen = end - start;
            if (rangeLen < minSegLength) continue;
            
            segments.Add((start, end, next));
        }
        return segments;
    }
}