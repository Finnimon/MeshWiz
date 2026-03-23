using System.Numerics;
using System.Runtime.CompilerServices;

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
        IReadOnlyList<Node<TVec,TNum>> Nodes { get; }
        bool IsTransforming => false;
        int Depth { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TraverseBvh<TTraverser, TIntersection>(TTraverser traverser) 
            where TTraverser : ITraverser<TElement, TIntersection, TVec, TNum>, allows ref struct =>
            Bvh.Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(this, traverser);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TraverseBvh<TIntersection>(
            Func<AABB<TVec>, bool> bBoxDoIntersect,
            Func<TElement, (TIntersection, bool)> elementIntersect,
            Func<int, TElement, TIntersection, HitReact> acceptHitReact
        ) => Bvh.Traverse(this, bBoxDoIntersect, elementIntersect, acceptHitReact);
    }
}