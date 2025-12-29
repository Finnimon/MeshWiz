using System.Numerics;
using System.Runtime.InteropServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Tetrahedron<TNum> : IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vec3<TNum> A, B, C, D;
    public Vec3<TNum> Centroid => (A + B + C + D)/TNum.CreateTruncating(4);
    public TNum Volume => TNum.Abs(CalculateSignedVolume(A, B, C, D));
    public TNum SurfaceArea => CalculateSurf();
    public ISurface<Vec3<TNum>, TNum> Surface => this;
    public AABB<Vec3<TNum>> BBox => AABB<Vec3<TNum>>.From(A,B,C,D);


    public Tetrahedron(Triangle3<TNum> triangle) 
        : this(triangle.A,  triangle.B, triangle.C, Vec3<TNum>.Zero)
    { }
    public Tetrahedron(Triangle3<TNum> triangle, Vec3<TNum> tip) 
        : this(triangle.A, triangle.B, triangle.C, tip)
    { }

    public Tetrahedron(Vec3<TNum> a,Vec3<TNum> b, Vec3<TNum> c, Vec3<TNum> d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }
    
    
    
    private TNum CalculateSurf()
    {
        var abc = new Triangle<Vec3<TNum>, TNum>(A, B, C).SurfaceArea;
        var dab = new Triangle<Vec3<TNum>, TNum>(D,A,B).SurfaceArea;
        var cda = new Triangle<Vec3<TNum>, TNum>(C,D,A).SurfaceArea;
        var bcd = new Triangle<Vec3<TNum>, TNum>(B,C,D).SurfaceArea;
        return abc + dab + cda + bcd;
    }

    public static TNum CalculateSignedVolume(Vec3<TNum> a, Vec3<TNum> b, Vec3<TNum> c, Vec3<TNum> d)
    {
        var ab = b - a;
        var ac = c - a;
        var ad = d - a;
        return Vec3<TNum>.Cross(ac, ad).Dot(ab) * Numbers<TNum>.Sixth;
    }

    public Triangle3<TNum>[] TessellatedSurface => [
        new(A, B, C),
        new(D, A, B),
        new(C, D, A),
        new(B, C, D),
    ];
    public IMesh<TNum> Tessellate() 
        => new IndexedMesh<TNum>([A,B,C,D], [
                new(0,1,2),
                new (3,0,1),
                new(2,3,0),
                new(1,2,3)
            ]);
}