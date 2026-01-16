using System.Numerics;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public readonly record struct Info<TVec, TNum>(
        Node<TVec, TNum>[] Nodes,
        int[]? IndexShuffle,
        int Depth
    )
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>;
}