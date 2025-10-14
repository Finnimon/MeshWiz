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

        public static IndexedMesh<TNum> Tessellate<TNum>(
            Polyline<Vector3<TNum>, TNum> sweepCurve, Ray3<TNum> axis, int ribCount, bool parallel = false)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(ribCount, 3);

            var pointCount = sweepCurve.Points.Length;

            var spines = new Vector3<TNum>[ribCount][];

            var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(ribCount);
            var origin = axis.Origin;
            var direction = axis.Direction;
            
            spines[0] = sweepCurve.Points[..];
            if (!parallel)
                for (var rib = 1; rib < ribCount; rib++)
                    TessellateRib(sweepCurve, rib, direction, angleStep, spines, pointCount, origin);
            else
                Parallel.For(1, ribCount,
                    rib => TessellateRib(sweepCurve, rib, direction, angleStep, spines, pointCount, origin));
            return Mesh.Create.LoftRibsClosed(spines);
        }

        private static void TessellateRib<TNum>(Polyline<Vector3<TNum>, TNum> sweepCurve, int rib, Vector3<TNum> direction, TNum angleStep,
            Vector3<TNum>[][] spines, int pointCount, Vector3<TNum> origin) where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var ribFactor = TNum.CreateTruncating(rib);
            var rot = Matrix4x4<TNum>.CreateRotation(direction, angleStep * ribFactor);
            var newPoints = sweepCurve.Points[..];
            spines[rib] = newPoints;
            for (var i = 0; i < pointCount; i++)
                newPoints[i] = rot.MultiplyDirection(newPoints[i] - origin) + origin;
        }
    }
}