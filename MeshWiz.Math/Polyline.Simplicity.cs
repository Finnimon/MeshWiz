using System.Numerics;
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
        public static Level MultiCheck<TNum>(Polyline<Vector2<TNum>, TNum> polygon)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var level = WindingDirectionCheck(polygon);
            if (level != Level.Unknown) return level;

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
                    if (!Line.TryIntersectOnSegment(l0, l1, out var t))
                        continue;
                    if (i != 0 || j != polygon.Count - 1)
                        return Level.Complex;

                    Line.TryIntersectOnSegment(l1, l0, out var t2);
                    if (t.IsApprox(TNum.Zero) && t2.IsApprox(TNum.One)) return Level.Simple;
                    var quickHeal = l1.Traverse(t);
                    polygon.Points[0] = quickHeal;
                    polygon.Points[^1] = quickHeal;
                }
            }

            return Level.Simple;
        }
    }
}