using System.Numerics;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Surface
{
    public static class Rotational
    {
        public static IndexedMesh<TNum> Tessellate<TRot, TNum>(TRot surface, int ribCount, bool parallel = false)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TRot : IRotationalSurface<TNum>
            => Tessellate(surface.SweepCurve.ToPolyline(), surface.SweepAxis, ribCount);

        public static IndexedMesh<TNum> Tessellate<TNum>(Vec2<TNum>[] sweepCurve, Ray3<TNum> axis,
            int ribCount, bool parallel = false)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(ribCount, 3);


            var spines = new Vec3<TNum>[ribCount][];

            var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(ribCount);
            
            var plane = new Plane3<TNum>(axis.Direction, axis.Origin);
            var u = plane.Basis.U;
            if (!parallel)
                for (var rib = 0; rib < ribCount; rib++)
                    TessellateRib(sweepCurve, rib, axis, angleStep, spines, u);
            else
                Parallel.For(0, ribCount,rib=> TessellateRib(sweepCurve, rib, axis, angleStep, spines, u));
            return Mesh.Create.LoftRibsClosed(spines);
        }

        private static void TessellateRib<TNum>(ReadOnlySpan<Vec2<TNum>> sourcePoints, int rib,
            Ray3<TNum> axis, TNum angleStep,
            Vec3<TNum>[][] spines,Vec3<TNum> basisU )
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            
            var ribFactor = TNum.CreateTruncating(rib);
            var rot = Matrix4x4<TNum>.CreateRotation(axis.Direction, angleStep * ribFactor);
            basisU = rot.MultiplyDirection(basisU);
            var newPoints = new Vec3<TNum>[sourcePoints.Length];
            spines[rib] = newPoints;
            for (var i = 0; i < sourcePoints.Length; i++)
            {
                var p = sourcePoints[i];
                var radius = p.Y;
                var t = p.X;
                var basePt = axis.Traverse(t);
                var radialShift = basisU * radius;
                newPoints[i] = basePt+radialShift;
            }
        }


        public static IndexedMesh<TNum> Tessellate<TNum>(
            Polyline<Vec3<TNum>, TNum> sweepCurve, Ray3<TNum> axis, int ribCount, bool parallel = false)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(ribCount, 3);

            var pointCount = sweepCurve.Points.Length;

            var spines = new Vec3<TNum>[ribCount][];

            var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(ribCount);
            var origin = axis.Origin;
            var direction = axis.Direction;
            
            spines[0] = sweepCurve.Points.ToArray();
            if (!parallel)
                for (var rib = 1; rib < ribCount; rib++)
                    TessellateRib(sweepCurve, rib, direction, angleStep, spines, pointCount, origin);
            else
                Parallel.For(1, ribCount,
                    rib => TessellateRib(sweepCurve, rib, direction, angleStep, spines, pointCount, origin));
            return Mesh.Create.LoftRibsClosed(spines);
        }

        private static void TessellateRib<TNum>(Polyline<Vec3<TNum>, TNum> sweepCurve, int rib, Vec3<TNum> direction, TNum angleStep,
            Vec3<TNum>[][] spines, int pointCount, Vec3<TNum> origin) where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribFactor = TNum.CreateTruncating(rib);
            var rot = Matrix4x4<TNum>.CreateRotation(direction, angleStep * ribFactor);
            
            var newPoints = sweepCurve.Points.ToArray();
            spines[rib] = newPoints;
            for (var i = 0; i < pointCount; i++)
                newPoints[i] = rot.MultiplyDirection(newPoints[i] - origin) + origin;
        }
    }
}