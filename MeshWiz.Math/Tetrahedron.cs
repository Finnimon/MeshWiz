using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Tetrahedron<TNum> : IBody<TNum>
    where TNum : unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public readonly Vector3<TNum> A, B, C, D;
    public Vector3<TNum> Centroid => (A + B + C + D)/TNum.CreateTruncating(4);
    public TNum Volume => CalculateVolume();
    public TNum SurfaceArea => CalculateSurf();
    public IFace<Vector3<TNum>, TNum>[] Surface => [..TessellatedSurface];
    public BBox3<TNum> BBox => GetBBox();

    private BBox3<TNum> GetBBox()
    {
        var(xMin,yMin,zMin) = A;
        var(xMax,yMax,zMax) = A;
        var (x, y, z) = B;
        xMin=TNum.Min(xMin,x);
        yMin=TNum.Min(yMin,y);
        zMin=TNum.Min(zMin,z);
        xMax=TNum.Max(xMax,x);
        yMax=TNum.Max(yMax,y);
        zMax=TNum.Max(zMax,z);
        (x, y, z) = C;
        xMin=TNum.Min(xMin,x);
        yMin=TNum.Min(yMin,y);
        zMin=TNum.Min(zMin,z);
        xMax=TNum.Max(xMax,x);
        yMax=TNum.Max(yMax,y);
        zMax=TNum.Max(zMax,z);
        (x, y, z) = D;
        xMin=TNum.Min(xMin,x);
        yMin=TNum.Min(yMin,y);
        zMin=TNum.Min(zMin,z);
        xMax=TNum.Max(xMax,x);
        yMax=TNum.Max(yMax,y);
        zMax=TNum.Max(zMax,z);
        
        return new BBox3<TNum>(new Vector3<TNum>(xMin,yMin,zMin), new Vector3<TNum>(xMax,yMax,zMax));
    }

    public Triangle3<TNum>[] TessellatedSurface => [
        new(A, B, C),
        new(D, A, B),
        new(C, D, A),
        new(B, C, D),
    ];

    public Tetrahedron(in Triangle3<TNum> triangle) 
        : this(in triangle.A, in triangle.B, in triangle.C, Vector3<TNum>.Zero)
    { }
    public Tetrahedron(in Triangle3<TNum> triangle,in Vector3<TNum> tip) 
        : this(in triangle.A, in triangle.B, in triangle.C, tip)
    { }

    public Tetrahedron(in Vector3<TNum> a, in Vector3<TNum> b, in Vector3<TNum> c, in Vector3<TNum> d)
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
    private TNum CalculateVolume()
    {
        var ab = B - A;
        var ac = C - A;
        var ad = D - A;
        return TNum.Abs(ab*(ac^ad)) / TNum.CreateTruncating(6);
    }
}