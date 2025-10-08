using System.Numerics;

namespace MeshWiz.Math;

public static partial class Surface
{
    public static class Rotational
    {
        public static IndexedMesh<TNum> Tessellate<TRot, TNum>(TRot surface, int ribCount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            where TRot : IRotationalSurface<TNum>
            => Tessellate(surface.SweepCurve.ToPolyline(), surface.SweepAxis, ribCount);

        public static IndexedMesh<TNum> Tessellate<TNum>(
            Polyline<Vector3<TNum>, TNum> sweepCurve, Ray3<TNum> axis, int ribCount)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(ribCount, 3);

            // Base curve tessellation
            var pointCount = sweepCurve.Points.Length;

            var spines = new Vector3<TNum>[ribCount][];

            var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(ribCount);
            var origin = axis.Origin;
            var direction = axis.Direction;

            // First rib = unrotated
            spines[0] = sweepCurve.Points.ToArray();
            // Remaining ribs
            for (var rib = 1; rib < ribCount; rib++)
            {
                var ribFactor = TNum.CreateTruncating(rib);
                var rot = Matrix4x4<TNum>.CreateRotation(direction, angleStep * ribFactor);
                var newPoints = sweepCurve.Points[..];
                spines[rib] = newPoints;
                for (var i = 0; i < pointCount; i++)
                    newPoints[i] = rot.MultiplyDirection(newPoints[i] - origin) + origin;
            }

            return Mesh.Create.LoftRibsClosed(spines);
        }
    }
}