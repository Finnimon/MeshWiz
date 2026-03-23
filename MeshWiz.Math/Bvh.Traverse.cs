using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;

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
        IReadOnlyList<Node<TVec, TNum>> nodes,
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
        return Traverse<FTraverser<TElement, TIntersection, TVec, TNum>, TElement, TIntersection, TVec, TNum>(elements,
            nodes, traverser, depth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(
        IHierarchy<TElement, TVec, TNum> hierarchy,
        TTraverser traverser
    )
        where TTraverser : ITraverser<TElement, TIntersection, TVec, TNum>, allows ref struct
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
        => Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(hierarchy.Elements, hierarchy.Nodes, traverser,
            hierarchy.Depth);

    public static bool Traverse<TTraverser, TElement, TIntersection, TVec, TNum>(
        IReadOnlyList<TElement> elements,
        IReadOnlyList<Node<TVec, TNum>> nodes,
        TTraverser traverser,
        int depth
    )
        where TTraverser : ITraverser<TElement, TIntersection, TVec, TNum>, allows ref struct
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        using var rented = RentedArray<int>.Rent(int.Max(1, depth));
        Span<int> stack = rented.GetCompleteArray();
        stack[0] = 0;
        var stackSize = 1;
        var didHit = false;
        while (0 < stackSize)
        {
            var nIndex = stack[--stackSize];
            nextNode:
            var n = nodes[nIndex];

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

    public static bool TraverseAgainst<TElement, TVec, TNum>(
        IHierarchy<TElement, TVec, TNum> left,
        IHierarchy<TElement, TVec, TNum> right,
        bool ignoreTouching,
        Func<TElement, TElement, bool> elemTest)
        where TVec : unmanaged, IVec<TVec, TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (left.Elements.Count == 0 || right.Elements.Count == 0) return false;
        if (!left.IsTransforming && right.IsTransforming) (left, right) = (right, left);
        using var leftRentedArr = RentedArray<int>.Rent(left.Depth + right.Depth);
        var leftStack = leftRentedArr.GetCompleteArray().AsSpan();
        using var rightRentedArr = RentedArray<int>.Rent(left.Depth + right.Depth);
        var rightStack = rightRentedArr.GetCompleteArray().AsSpan();
        leftStack[0] = 0;
        rightStack[0] = 0;
        var stackSize = 1;

        var leftElems = left.Elements;
        var rightElems = right.Elements;
        var leftNodes = left.Nodes;
        var rightNodes = right.Nodes;
        while (0 < stackSize)
        {
            var leftPos = leftStack[--stackSize];
            var rightPos = rightStack[stackSize];
            var leftNode = leftNodes[leftPos];
            var rightNode = rightNodes[rightPos];

            afterNodePop:
            var separated = false;
            var touching = false;

            for (var i = 0; i < TVec.Dimensions; i++)
            {
                var dimA = leftNode.Bounds.GetDim<TVec, TNum>(i);
                var dimB = rightNode.Bounds.GetDim<TVec, TNum>(i);

                if (!dimA.Max.IsApproxGreaterOrEqual(dimB.Min) || !dimB.Max.IsApproxGreaterOrEqual(dimA.Min))
                {
                    separated = true;
                    break;
                }

                if (dimA.Max.IsApprox(dimB.Min) || dimB.Max.IsApprox(dimA.Min))
                    touching = true;
            }

            if (separated)
                continue;

            if (ignoreTouching && touching)
                continue;


            var leftLen = leftNode.Bounds.Size.SquaredLength;
            var rightLen = rightNode.Bounds.Size.SquaredLength;
            if (leftNode.IsParent && (rightNode.IsLeaf || rightLen <= leftLen))
            {
                leftStack[stackSize] = leftNode.SecondChild;
                rightStack[stackSize++] = rightPos;
                leftNode = leftNodes[(leftPos = leftNode.FirstChild)];

                goto afterNodePop;
            }

            if (rightNode.IsParent)
            {
                leftStack[stackSize] = leftPos;
                rightStack[stackSize++] = rightNode.SecondChild;
                rightNode = rightNodes[(rightPos = rightNode.FirstChild)];
                goto afterNodePop;
            }

            // @formatter:off
            for (var leftElem = 0; leftElem < leftNode.Length; leftElem++)
            {
            var leftElement = leftElems[leftElem + leftNode.Start];
            for (var rightElem = 0; rightElem < rightNode.Length; rightElem++)
            {
                var rightElement = rightElems[rightElem + rightNode.Start];
                var hit = elemTest(leftElement, rightElement);
                if (hit) return true;
            }
            }
            // @formatter:on
        }

        return false;
    }

    //{
    //     Span<(BVHNode<T> A, BVHNode<T> B)> stack =
    //         stackalloc (BVHNode<T>, BVHNode<T>)[maxDepthA + maxDepthB];
    // 
    //     int sp = 0;
    //     stack[sp++] = (rootA, rootB);
    // 
    //     while (sp > 0)
    //     {
    //         var (a, b) = stack[--sp];
    // 
    //         if (!IntersectsOrTouches(a.Bounds, b.Bounds))
    //             continue;
    // 
    //         if (a.IsLeaf && b.IsLeaf)
    //         {
    //             foreach (var triA in a.Triangles)
    //             foreach (var triB in b.Triangles)
    //                 if (TriangleIntersects(triA, triB))
    //                     return true;
    // 
    //             continue;
    //         }
    // 
    //         // Decide which side to split
    //         bool splitA =
    //             !a.IsLeaf &&
    //             (b.IsLeaf || a.Bounds.Volume >= b.Bounds.Volume);
    // 
    //         if (splitA)
    //         {
    //             stack[sp++] = (a.Left, b);
    //             stack[sp++] = (a.Right, b);
    //         }
    //         else
    //         {
    //             stack[sp++] = (a, b.Left);
    //             stack[sp++] = (a, b.Right);
    //         }
    //     }
    // 
    //     return false;
    // }
}