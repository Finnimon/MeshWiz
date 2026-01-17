using System.Numerics;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public interface IHierarchy<out TElement, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        /// <summary>
        /// must be ordered correctly!
        /// </summary>
        IReadOnlyList<TElement> Elements { get; }
        ReadOnlySpan<Node<TVec,TNum>> Nodes { get; }
        int Depth { get; }
    }
}