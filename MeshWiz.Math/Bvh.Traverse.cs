using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility;

namespace MeshWiz.Math;

public static partial class Bvh
{
    public interface ITraverser<in TElement, TIntersection, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        bool DoIntersect(AABB<TVec> t);
        bool Intersect(TElement element, out TIntersection intersection);
        HitReact AcceptHit(int index, TElement element, TIntersection hit);
    }

    public readonly struct FTraverser<TElement, TIntersection, TVec, TNum>
        : ITraverser<TElement, TIntersection, TVec, TNum>
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        private readonly Func<AABB<TVec>, bool> _bBoxDoIntersect;
        private readonly Func<TElement, (TIntersection intersection, bool doIntersect)> _elementIntersect;
        private readonly Func<int, TElement, TIntersection, HitReact> _acceptHitReact;

        /// <param name="bBoxDoIntersect"></param>
        /// <param name="elementIntersect"></param>
        /// <param name="acceptHitReact"></param>
        public FTraverser(Func<AABB<TVec>, bool> bBoxDoIntersect,
            Func<TElement, (TIntersection, bool)> elementIntersect,
            Func<int, TElement, TIntersection, HitReact> acceptHitReact)
        {
            _bBoxDoIntersect = bBoxDoIntersect;
            _elementIntersect = elementIntersect;
            _acceptHitReact = acceptHitReact;
        }
        
        
        /// <inheritdoc />
        public bool Intersect(TElement test, out TIntersection result)
        {
            (result, var hit) = _elementIntersect(test);
            return hit;
        }

        /// <inheritdoc />
        public bool DoIntersect(AABB<TVec> t) => _bBoxDoIntersect(t);

        /// <inheritdoc />
        public HitReact AcceptHit(int index, TElement element, TIntersection hit)
            => _acceptHitReact(index, element, hit);
    }

    public enum HitReact
    {
        BreakCompletely,
        BreakCurrentNode,
        ContinueCurrentNode
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Traverse<TElement, TIntersection, TVec, TNum>(
        IHierarchy<TElement, TVec, TNum> hierarchy,
        Func<AABB<TVec>, bool> bBoxDoIntersect,
        Func<TElement, (TIntersection, bool)> elementIntersect,
        Func<int, TElement, TIntersection, HitReact> acceptHitReact
    )
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => Traverse(hierarchy.Elements,
            hierarchy.Nodes,
            hierarchy.Depth,
            bBoxDoIntersect,
            elementIntersect,
            acceptHitReact);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Traverse<TElement, TIntersection, TVec, TNum>(
        IReadOnlyList<TElement> elements,
        ReadOnlySpan<Node<TVec, TNum>> nodes,
        int depth,
        Func<AABB<TVec>, bool> bBoxDoIntersect,
        Func<TElement, (TIntersection, bool)> elementIntersect,
        Func<int, TElement, TIntersection, HitReact> acceptHitReact,
        Func<TElement, bool>? elementDoIntersect = null
    )
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        FTraverser<TElement, TIntersection, TVec, TNum> traverser = new(
                bBoxDoIntersect,
                elementIntersect,
                acceptHitReact);
        return Traverse<FTraverser<TElement, TIntersection, TVec, TNum>, TElement, TIntersection, TVec, TNum>(elements, nodes, traverser, depth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(
        IHierarchy<TElement, TVec, TNum> hierarchy,
        TTraverser traverser
    )
        where TTraverser : ITraverser<TElement, TIntersection, TVec, TNum>, allows ref struct
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(hierarchy.Elements, hierarchy.Nodes, traverser, hierarchy.Depth);

    public static bool Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(
        IReadOnlyList<TElement> elements,
        ReadOnlySpan<Node<TVec, TNum>> nodes,
        TTraverser traverser,
        int depth
    )
        where TTraverser : ITraverser<TElement, TIntersection, TVec, TNum>, allows ref struct
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        using var rented = RentedArray<int>.Rent(int.Max(1,depth));
        Span<int> stack = rented.GetCompleteArray();
        stack[0] = 0;
        var stackSize = 1;
        var didHit = false;
        while (0 < stackSize)
        {
            var nIndex = stack[--stackSize];
            nextNode:            
            ref readonly var n =ref nodes[nIndex];

            var isHit = traverser.DoIntersect(n.Bounds);
            if (!isHit) continue;
            if (n.IsParent)
            {
                stack[stackSize++] = n.SecondChild;
                nIndex = n.FirstChild;
                goto nextNode; //faster depth first path avoids read write
            }

            var end = n.End;
            for (var i = n.Start; i < end; i++)
            {
                var elem = elements[i];
                var doHit = traverser.Intersect(elem, out var hit);
                if (!doHit) continue;
                var accept = traverser.AcceptHit(i, elem, hit);
                didHit = true;
                switch (accept)
                {
                    case HitReact.BreakCompletely:
                        return true;
                    case HitReact.BreakCurrentNode:
                        goto nextIteration;
                    case HitReact.ContinueCurrentNode:
                    default:
                        continue;
                }
            }

            nextIteration: ;
        }

        return didHit;
    }
}