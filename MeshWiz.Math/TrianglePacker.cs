using System.Numerics;

namespace MeshWiz.Math;

public static class TrianglePacker<TNum>
where TNum:unmanaged, IBinaryFloatingPointIeee754<TNum>
{
    public static List<Triangle2<TNum>[]> Pack(IReadOnlyList<Triangle3<TNum>[]> triangles, TNum spacing)
    {
        throw new NotImplementedException();
    }
}