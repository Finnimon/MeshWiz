using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Ray3<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum> 
{
    public readonly Vector3<TNum> Origin, Direction;
    
    public Ray3( Vector3<TNum> origin, Vector3<TNum> direction)
    {
        Origin = origin;
        Direction = direction.Normalized;
    }
    
    
    public Vector3<TNum> Traverse(TNum distance)
        =>Origin+Direction*distance;
    

    public bool HitTest(Plane3<TNum> plane)
    {
        var dot= plane.Normal * Direction;
        return dot >TNum.Epsilon;
    }
    public bool HitTest(Plane3<TNum> plane, out TNum t)
    {
        var denominator = plane.Normal * Direction;

        // Check if ray is parallel to the plane
        if (TNum.Abs(denominator) < TNum.Epsilon)
        {
            t = TNum.NaN;
            return false;
        }

        // Compute intersection distance along ray direction
        t = -(plane.Normal * Origin + plane.D) / denominator;

        // If t < 0, the intersection point is behind the ray's origin
        return t >= TNum.Zero;
    }
    
    public bool HitTest(in Triangle3<TNum> triangle, out TNum t)
    {
        t = TNum.NaN;

        var edge1 = triangle.B - triangle.A;
        var edge2 = triangle.C - triangle.A;
        var h = Direction ^ edge2; // cross product
        var a = edge1 * h;         // dot product

        if (TNum.Abs(a) < TNum.Epsilon)
            return false; // Ray is parallel to the triangle

        var f = TNum.One / a;
        var s = Origin - triangle.A;
        var u = f * (s * h);

        if (u < TNum.Zero || u > TNum.One)
            return false;

        var q = s ^ edge1;
        var v = f * (Direction * q);

        if (v < TNum.Zero || u + v > TNum.One)
            return false;

        t = f * (edge2 * q);

        return t >= TNum.Zero; // Intersection in ray direction
    }

    public bool HitTest(in BBox3<TNum> box, out TNum tNear, out TNum tFar)
    {
        tNear = TNum.NegativeInfinity;
        tFar = TNum.PositiveInfinity;

        for (int i = 0; i < 3; i++)
        {
            var origin = Origin[i];
            var dir = Direction[i];
            var min = box.Min[i];
            var max = box.Max[i];

            if (TNum.Abs(dir) < TNum.Epsilon && (origin < min || origin > max)) return false;
            var invD = TNum.One / dir;
            var t1 = (min - origin) * invD;
            var t2 = (max - origin) * invD;
            if (t1 > t2) (t1, t2) = (t2, t1);
            
            tNear = TNum.Max(tNear, t1);
            tFar = TNum.Min(tFar, t2);

            if (tNear > tFar || tFar < TNum.Zero) return false;
        }

        return true;
    }
    
    public static implicit operator Ray3<TNum>(in Line<Vector3<TNum>,TNum> line)
        =>new(line.Start, line.Direction);
}