using System.Numerics;
using JetBrains.Annotations;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Polyline
{
    public static class Simplicity
    {
        public static Level WindingDirectionCheck<TNum>(Polyline<Vector2<TNum>, TNum> polygon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (polygon.Count < 3) return Level.Simple;
            var centroid = polygon.VertexCentroid;
            var orientation = 0;
            foreach (var line in polygon)
            {
                var curOrientation = OrientationTo(line, centroid);
                if (curOrientation == 0) continue;
                if (orientation == 0) orientation = curOrientation;
                else if (orientation != curOrientation) return Level.Unknown;
            }

            return Level.Simple;
        }

        private static int OrientationTo<TNum>(Line<Vector2<TNum>, TNum> line, Vector2<TNum> p)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var lineMid = line.MidPoint;
            var directionRight = line.Direction.Right;
            var pToLine = lineMid - p;
            return TNum.Sign(pToLine.Dot(directionRight));
        }

        public enum Level
        {
            Simple,
            Complex,
            Unknown
        }

        /// <summary>
        /// Uses different methods to check the simplicity of a polygon
        /// </summary>
        /// <param name="polygon"></param>
        /// <typeparam name="TNum"></typeparam>
        /// <returns></returns>
        [Pure]
        public static Level MultiCheck<TNum>(Polyline<Vector2<TNum>, TNum> polygon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            if (polygon.IsClosed && Evaluate.IsConvex(polygon)) return Level.Simple;
            return CompleteCheck(polygon);
        }


        /// <summary>
        /// Complete O(N^2) complexity check
        /// </summary>
        /// <remarks>Heals the ends if they are not properly touching</remarks>
        public static Level CompleteCheck<TNum>(Polyline<Vector2<TNum>, TNum> polygon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            for (var i = 0; i < polygon.Count - 2; i++)
            {
                var l0 = polygon[i];
                for (var j = i + 2; j < polygon.Count; j++)
                {
                    var l1 = polygon[j];
                    if (!Line.TryIntersectOnSegment(l0, l1, out var t, out var t2))
                        continue;
                    if (i != 0 || j != polygon.Count - 1)
                        return Level.Complex;
                    if (t.IsApprox(TNum.Zero) && t2.IsApprox(TNum.One)) continue;
                    return Level.Complex;
                }
            }

            return Level.Simple;
        }


        public static Polyline<Vector2<TNum>, TNum>[] MakeSimple<TNum>(
            Polyline<Vector2<TNum>, TNum> polygon,
            TNum epsilon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var simplicity = MultiCheck(polygon);
            if (simplicity == Level.Simple) return [polygon];
            return MakeSimpleSkippingPreCheck(polygon, epsilon);
        }

        private static Polyline<Vector2<TNum>, TNum>[] MakeSimpleSkippingPreCheck<TNum>(
            Polyline<Vector2<TNum>, TNum> polygon, TNum epsilon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var segmentPositions = Evaluate.FindIdentifiableSegments(polygon, epsilon);
            RollingList<Vector2<TNum>>? connected = null;
            var nextSeg = TNum.NaN;
            var sinceLastAdd = 0;
            RollingList<Polyline<Vector2<TNum>, TNum>> simplified = [];
            while (segmentPositions.TryPopFront(out var range))
            {
                if (range.start.IsApprox(range.next))
                {
                    simplified.Add(polygon.ExactSection(range.start, range.end));
                    continue;
                }

                if (connected is null || sinceLastAdd > segmentPositions.Count + 2 ||
                    connected[0].IsApprox(connected[^1], epsilon))
                {
                    if (connected is { Count: > 1 })
                        simplified.Add(new Polyline<Vector2<TNum>, TNum>(connected.ToArray()));
                    connected = new(polygon.ExactSection(range.start, range.end).Points);
                    nextSeg = range.next;
                    sinceLastAdd = 0;
                    continue;
                }

                if (!range.start.IsApprox(nextSeg, epsilon))
                {
                    segmentPositions.PushBack(range);
                    sinceLastAdd++;
                    continue;
                }

                sinceLastAdd = 0;
                connected.PushBack(polygon.ExactSection(range.start, range.end).Points.AsSpan(1));
            }
            
            if (connected is { Count: > 1 }) simplified.Add(new Polyline<Vector2<TNum>, TNum>(connected.ToArray()));

            return Creation.UnifyNonReversing(simplified, epsilon);
        }
    }
}