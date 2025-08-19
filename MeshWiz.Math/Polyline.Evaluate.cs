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


        public static WindingOrder GetWindingOrderAreaSign<TNum>(Polyline<Vector2<TNum>, TNum> polyline)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            => AreaSign(polyline) switch
            {
                -1 => WindingOrder.Clockwise,
                0 => WindingOrder.NotClosed,
                1 => WindingOrder.CounterClockwise,
                _ => throw new InvalidOperationException(nameof(AreaSign))
            };

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

            var prevIndex = (index - 1) % (polyline.Points.Length - 1);
            var nextIndex = (index + 1) % (polyline.Points.Length - 1);
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


        /// <param name="polyline">source polyline</param>
        /// <param name="ccw"> resulting CCW oriented Polylines</param>
        /// <param name="cw">resulting CW oriented Polylines</param>
        /// <returns>Whether any splits where possible</returns>
        public static bool TrySplitAlongSelfIntersections<TNum>(
            Polyline<Vector2<TNum>, TNum> polyline,
            out Polyline<Vector2<TNum>, TNum>[]? ccw,
            out Polyline<Vector2<TNum>, TNum>[]? cw)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            List<Range> segments = [];
            throw new NotImplementedException();
        }
    }
}