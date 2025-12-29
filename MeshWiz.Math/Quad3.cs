using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Quad3<TNum>(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> c, Vec3<TNum> d)
    :ISurface3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> A = a, B = b, C = c, D = d;
    public Vec3<TNum> Centroid => (A + B + C + D)/TNum.CreateTruncating(4);
    public TNum SurfaceArea =>new Triangle3<TNum>(A,B,D).SurfaceArea+new Triangle3<TNum>(B,C,D).SurfaceArea;

    public Quad3<TNum> Shift(Vec3<TNum> shift) => new(A+shift, B+shift, C+shift, D+shift);
    public AABB<Vec3<TNum>> BBox =>AABB<Vec3<TNum>>.From(A,B,C,D);
    public IMesh<TNum> Tessellate()
    {
        Vec3<TNum>[] vertices = [A,B,C,D];
        TriangleIndexer[] indices = [new(0, 1, 3), new(1, 2, 3)];
        return new IndexedMesh<TNum>(vertices, indices);
    }
}