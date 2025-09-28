using System.Numerics;

namespace MeshWiz.Math;

public static partial class Surface
{
    public static class Rotational
    {
        public static IndexedMesh<TNum> Tessellate<TRot, TNum>(
            TRot surf, int ribCount)
            where TRot : IRotationalSurface<TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(ribCount, 3);

            // Base curve tessellation
            var sweepCurve = surf.SweepCurve.ToPolyline();
            var pointCount = sweepCurve.Points.Length;

            var spines = new Vector3<TNum>[ribCount][];

            var angleStep = Numbers<TNum>.TwoPi / TNum.CreateTruncating(ribCount);
            var axis = surf.SweepAxis;
            var origin = axis.Origin;
            var direction = axis.Direction;

            // First rib = unrotated
            spines[0] = sweepCurve.Points.ToArray();
            var c = sweepCurve.Points[1];
            // Remaining ribs
            for (var rib = 1; rib < ribCount; rib++)
            {
                var ribFactor = TNum.CreateTruncating(rib);
                var rot = Matrix4<TNum>.CreateRotation(direction, angleStep * ribFactor);
                var newPoints = sweepCurve.Points[..];
                spines[rib] = newPoints;
                for (var i = 0; i < pointCount; i++) 
                    newPoints[i] = rot.MultiplyDirection(newPoints[i] - origin) + origin;
            }

            return Mesh.Create.LoftRibsClosed(spines);
        }
    }
}