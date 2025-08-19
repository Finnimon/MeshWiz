using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Quad3<TNum>(Vector3<TNum> a, Vector3<TNum> b, Vector3<TNum> c, Vector3<TNum> d)
    :ISurface3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> A = a, B = b, C = c, D = d;
    public Vector3<TNum> Centroid => (A + B + C + D)/TNum.CreateTruncating(4);
    public TNum SurfaceArea =>new Triangle3<TNum>(A,B,D).SurfaceArea+new Triangle3<TNum>(B,C,D).SurfaceArea;

    public Quad3<TNum> Shift(Vector3<TNum> shift) => new(A+shift, B+shift, C+shift, D+shift);
    public BBox3<TNum> BBox =>BBox3<TNum>.FromPoint(A).CombineWith(B).CombineWith(C).CombineWith(D);
    public IMesh<TNum> Tessellate()
    {
        Vector3<TNum>[] vertices = [A,B,C,D];
        TriangleIndexer[] indices = [new(0, 1, 3), new(1, 2, 3)];
        return new IndexedMesh<TNum>(vertices, indices);
    }
}