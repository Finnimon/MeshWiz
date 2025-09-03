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

    /// <summary>
    /// Finds the shortest cross-section at indents
    /// </summary>
    /// <param name="polyline"></param>
    /// <param name="windingOrder"></param>
    /// <typeparam name="TNum"></typeparam>
    /// <returns></returns>
    public static TNum ShortestCrossSection<TNum>(Polyline<Vector2<TNum>, TNum> polyline, WindingOrder windingOrder)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        throw new NotImplementedException();
    }
}