using System.Numerics;

namespace MeshWiz.Math;

public static partial class PlaneSolver
{
    /// <summary>
    /// Computes a best-fit direction v such that offsetting each plane by v
    /// moves them approximately one unit along their normals in least-squares sense.
    /// Only planes within the given range are considered.
    /// If the range is empty, returns the zero vector.
    /// If the normal matrix is singular (e.g., all planes parallel), returns the first plane's normal.
    /// </summary>
    public static Vector3<TNum> SolveBestFitPlaneIntersectionNormal<TNum>(
        IReadOnlyList<Plane3<TNum>> planes,
        Range range,
        TNum[,]? componentBuffer=null)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        int countAll = planes.Count;
        var (start, length) = range.GetOffsetAndLength(countAll);
        if (length == 0)
            return Vector3<TNum>.Zero;

        // Accumulate normal outer-products and normals
        var components = componentBuffer ?? new TNum[3, 3];
        Vector3<TNum> b = Vector3<TNum>.Zero;

        for (int i = start; i < start + length; i++)
        {
            var n = planes[i].Normal;
            components[0, 0] += n.X * n.X;
            components[0, 1] += n.X * n.Y;
            components[0, 2] += n.X * n.Z;
            components[1, 0] += n.Y * n.X;
            components[1, 1] += n.Y * n.Y;
            components[1, 2] += n.Y * n.Z;
            components[2, 0] += n.Z * n.X;
            components[2, 1] += n.Z * n.Y;
            components[2, 2] += n.Z * n.Z;

            b += n;
        }

        var m = Matrix3<TNum>.FromComponents(components);
        // Solve M * v = b
        return TNum.Abs(m.Det) < TNum.CreateChecked(1e-6) ? planes[start].Normal : m.Solve(b);
    }
}