using System.Numerics;
using System.Runtime.InteropServices;

namespace MeshWiz.Math;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Plane3<TNum> where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public readonly TNum D;
    public readonly Vector3<TNum> Normal;

    public Plane3(in Triangle3<TNum> triangleOnPlane)
    {
        Normal=triangleOnPlane.Normal;
        D = -Normal * triangleOnPlane.A;
    }
    
    public Plane3(in Vector3<TNum> a, in Vector3<TNum> b, in Vector3<TNum> c)
    {
        Normal=(a-b)^(c-a);
        Normal=Normal.Normalized;
        D = -Normal * a;
    }

    public Plane3(in Vector4<TNum> asVec4) : this(asVec4.XYZ, asVec4.W) { }
    
    public Plane3(Vector3<TNum> normal, TNum d)
    {
        Normal = normal;
        D = d;
    }
}