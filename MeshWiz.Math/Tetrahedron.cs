using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Tetrahedron<TNum> : IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> A, B, C, D;
    public Vector3<TNum> Centroid => (A + B + C + D)/TNum.CreateTruncating(4);
    public TNum Volume => TNum.Abs(CalculateSignedVolume(A, B, C, D));
    public TNum SurfaceArea => CalculateSurf();
    public ISurface<Vector3<TNum>, TNum> Surface => this;
    public BBox3<TNum> BBox => BBox3<TNum>.FromPoint(A)
        .CombineWith(B)
        .CombineWith(C)
        .CombineWith(D);


    public Tetrahedron(Triangle3<TNum> triangle) 
        : this(triangle.A,  triangle.B, triangle.C, Vector3<TNum>.Zero)
    { }
    public Tetrahedron(Triangle3<TNum> triangle, Vector3<TNum> tip) 
        : this(triangle.A, triangle.B, triangle.C, tip)
    { }

    public Tetrahedron(Vector3<TNum> a,Vector3<TNum> b, Vector3<TNum> c, Vector3<TNum> d)
    {
        A = a;
        B = b;
        C = c;
        D = d;
    }
    
    
    
    private TNum CalculateSurf()
    {
        var abc = new Triangle<Vector3<TNum>, TNum>(A, B, C).SurfaceArea;
        var dab = new Triangle<Vector3<TNum>, TNum>(D,A,B).SurfaceArea;
        var cda = new Triangle<Vector3<TNum>, TNum>(C,D,A).SurfaceArea;
        var bcd = new Triangle<Vector3<TNum>, TNum>(B,C,D).SurfaceArea;
        return abc + dab + cda + bcd;
    }

    public static TNum CalculateSignedVolume(Vector3<TNum> a, Vector3<TNum> b, Vector3<TNum> c, Vector3<TNum> d)
    {
        var ab = b - a;
        var ac = c - a;
        var ad = d - a;
        return ab*(ac^ad) / TNum.CreateTruncating(6);
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