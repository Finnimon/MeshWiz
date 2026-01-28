using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using MeshWiz.RefLinq;
using MeshWiz.Utility;
using MeshWiz.Utility.Extensions;
using BvhStep = (int ParentIndex, int Depth);


namespace MeshWiz.Math;

public static partial class Bvh
{
    
    public static partial class Create
    {
        private readonly record struct Split<TVec, TNum>(
            int Axis,
            TNum Level,
            TNum Cost,
            AABB<TVec> Left,
            AABB<TVec> Right
        ) where TVec : unmanaged, IFloatingPointIeee754<TVec>;

        public static Info<TVec, TNum> Sah<TBounded, TVec, TNum>(
            IReadOnlyList<TBounded> source,
            int maxDepth = 32,
            int splitTests = 4,
            int minNodeSize = 1
        )
            where TBounded : IBounded<TVec>
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            maxDepth = int.Max(1, maxDepth);
            minNodeSize = int.Max(1, minNodeSize);
            var perItemBounds = source
                .Select(item => item.BBox)
                .ToArray();
            var perItemPosition = perItemBounds
                .Select(b => b.Center)
                .ToArray();
            var n = perItemBounds.Length;
            var indexShuffle = Enumerable.Range(0, n).ToArray();
            var shuffledIndexSpan = indexShuffle.AsSpan();
            var shuffledBounds = shuffledIndexSpan
                .Select(i => perItemBounds[i]);
            var shuffledPositions = shuffledIndexSpan
                .Select(i => perItemPosition[i]);
            var rootBox = AABB.Combine(perItemBounds);
            List<Node<TVec, TNum>> nodes = [Node<TVec, TNum>.MakeLeaf(rootBox, 0, n)];
            using var rentSpace = RentedArray<BvhStep>.Rent(maxDepth);
            Span<BvhStep> stack = rentSpace.GetCompleteArray();
            var stackSize = 0;
            stack[stackSize++] = (0, 1);
            var resultDepth = 1;
            while (0<stackSize)
            {
                var (parentIndex, depth) = stack[--stackSize];
                var parent = nodes[parentIndex];

                if (parent.Length <= minNodeSize)
                    continue;
                var range = parent.LeafRange;

                var (axis, splitLevel, cost, bbLeft, bbRight) = ChooseSplit<TVec, TNum>(
                    parent.Bounds,
                    shuffledBounds[range],
                    shuffledPositions[range],
                    splitTests
                );
                if (cost >= parent.LeafCost)
                    continue;

                var i = parent.Start;
                var j = parent.End - 1;
                while (i <= j)
                {
                    while (i <= j && shuffledPositions[i][axis] <= splitLevel) i++;
                    while (i <= j && shuffledPositions[j][axis] > splitLevel) j--;
                    if (i >= j) continue;
                    shuffledIndexSpan.Swap(i, j);
                    i++;
                    j--;
                }

                var leftChildLength = i - parent.Start;

                if (leftChildLength.OutsideInclusiveRange(0, parent.Length - 1)) continue;


                var leftChild = Node<TVec, TNum>.MakeLeaf(bbLeft, parent.Start, leftChildLength);
                var rightChild = Node<TVec, TNum>.MakeLeaf(bbRight, leftChild.End, parent.Length - leftChildLength);
                var leftIndex = nodes.Count;
                nodes.Add(leftChild);
                var rightIndex = leftIndex + 1;
                nodes.Add(rightChild);

                nodes[parentIndex] = parent.WithChildren(leftIndex, rightIndex);
                ++depth;
                resultDepth = int.Max(depth, resultDepth);

                if (depth >= maxDepth) continue;
                stack[stackSize++] = (rightIndex,depth);
                stack[stackSize++] = (leftIndex,depth);
            }

            return new Info<TVec, TNum>(nodes.ToArray(), indexShuffle, resultDepth);
        }

        public static Info<TVec, TNum> SahNonReordering<TBounded, TVec, TNum>(
            IReadOnlyList<TBounded> source,
            int maxDepth = 32,
            int splitTests = 4,
            int minNodeSize = 1
        )
            where TBounded : IBounded<TVec>
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            minNodeSize = int.Max(1, minNodeSize);
            var perItemBounds = source
                .Select(item => item.BBox)
                .ToArray();
            var boundsSpan = perItemBounds.AsSpan();

            var n = perItemBounds.Length;
            var rootBox = AABB.Combine(perItemBounds);
            List<Node<TVec, TNum>> nodes = [Node<TVec, TNum>.MakeLeaf(rootBox, 0, n)];
            using var rentSpace = RentedArray<BvhStep>.Rent(maxDepth);
            Span<BvhStep> stack = rentSpace.GetCompleteArray();
            var stackSize = 0;
            stack[stackSize++] = (0, 1);
            var resultDepth = 1;
            while (0<stackSize)
            {
                var (parentIndex, depth) = stack[--stackSize];
                var parent = nodes[parentIndex];

                if (parent.Length <= minNodeSize)
                    continue;
                var curBounds = boundsSpan[parent.Start..parent.End];
                var (cost, bbLeft, bbRight, leftLength) =
                    ChooseNonReorderedSplit<TVec, TNum>(parent.Bounds, curBounds, splitTests);
                if (cost >= parent.LeafCost)
                    continue; //do not split

                var leftChild = Node<TVec, TNum>.MakeLeaf(bbLeft, parent.Start, leftLength);
                var rightChild = Node<TVec, TNum>.MakeLeaf(bbRight, leftChild.End, parent.Length - leftLength);
                var leftIndex = nodes.Count;
                nodes.Add(leftChild);
                var rightIndex = leftIndex + 1;
                nodes.Add(rightChild);
                nodes[parentIndex] = parent.WithChildren(leftIndex, rightIndex);
                ++depth;
                resultDepth = int.Max(depth, resultDepth);

                if (depth > maxDepth) continue;
                stack[stackSize++] = (rightIndex,depth);
                stack[stackSize++] = (leftIndex,depth);
            }

            return new Info<TVec, TNum>(nodes.ToArray(), null, resultDepth);
        }
        
public static Info<TVec, TNum> BinaryBalancedNonReordering<TBounded, TVec, TNum>(
            IReadOnlyList<TBounded> source,
            int maxDepth = 32,
            int minNodeSize = 1
        )
            where TBounded : IBounded<TVec>
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            minNodeSize = int.Max(1, minNodeSize);
            var perItemBounds = source
                .Select(item => item.BBox)
                .ToArray();
            var boundsSpan = perItemBounds.AsSpan();

            var n = perItemBounds.Length;
            var rootBox = AABB.Combine(perItemBounds);
            List<Node<TVec, TNum>> nodes = [Node<TVec, TNum>.MakeLeaf(rootBox, 0, n)];
            using var rentSpace = RentedArray<BvhStep>.Rent(maxDepth);
            Span<BvhStep> stack = rentSpace.GetCompleteArray();
            var stackSize = 0;
            stack[stackSize++] = (0, 1);
            var resultDepth = 1;
            while (0<stackSize)
            {
                var (parentIndex, depth) = stack[--stackSize];
                var parent = nodes[parentIndex];

                if (parent.Length <= minNodeSize)
                    continue;
                var curBounds = boundsSpan[parent.Start..parent.End];
                var leftLength = curBounds.Length / 2 + (curBounds.Length % 2);
                var bbLeft = AABB.Combine(curBounds[..leftLength]);
                var bbRight = AABB.Combine(curBounds[leftLength..]);
                var cost = SahLeafCost<TVec, TNum>(bbLeft.Size, leftLength)+SahLeafCost<TVec, TNum>(bbRight.Size, parent.Length-leftLength);
                if (cost >= parent.LeafCost)
                    continue; //do not split

                var leftChild = Node<TVec, TNum>.MakeLeaf(bbLeft, parent.Start, leftLength);
                var rightChild = Node<TVec, TNum>.MakeLeaf(bbRight, leftChild.End, parent.Length - leftLength);
                var leftIndex = nodes.Count;
                nodes.Add(leftChild);
                var rightIndex = leftIndex + 1;
                nodes.Add(rightChild);
                nodes[parentIndex] = parent.WithChildren(leftIndex, rightIndex);
                ++depth;
                resultDepth = int.Max(depth, resultDepth);

                if (depth > maxDepth) continue;
                stack[stackSize++] = (rightIndex,depth);
                stack[stackSize++] = (leftIndex,depth);
            }

            return new Info<TVec, TNum>(nodes.ToArray(), null, resultDepth);
        }
        private static (TNum cost, AABB<TVec> bbLeft, AABB<TVec> bbRight, int leftLength) ChooseNonReorderedSplit<TVec,
            TNum>(
            AABB<TVec> outer,
            ReadOnlySpan<AABB<TVec>> bounds,
            int splitTests
        )
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var (_, _, bestCost, leftBounds, rightBounds) = BadSplit<TVec, TNum>();
            var bestLeftLength = -1;
            var n = bounds.Length;
            var bestSplit = BadSplit<TVec, TNum>();
            if (bounds.Length <= 1)
                return (bestCost, leftBounds, rightBounds, bestLeftLength);
            var step = 1.0 / splitTests;
            var parentCost = SahLeafCost<TVec, TNum>(outer.Size, n);
            for (var test = 1; test < splitTests + 1; test++)
            {
                var leftLength = (int)double.Ceiling(test * step * n);
                var rightLength = n - leftLength;
                if (rightLength == 0 || leftLength == 0) continue;
                var curLeftBounds = bounds[..leftLength].Iterate().Aggregate(AABB<TVec>.Combine);
                var curRightBounds = bounds[leftLength..].Iterate().Aggregate(AABB<TVec>.Combine);
                var leftCost = SahLeafCost<TVec, TNum>(curLeftBounds.Size, leftLength);
                var rightCost = SahLeafCost<TVec, TNum>(curRightBounds.Size, rightLength);
                var curTotalCost = leftCost + rightCost;
                if (curTotalCost >= parentCost)
                    continue;
                if (curTotalCost >= bestCost)
                    continue;
                leftBounds = curLeftBounds;
                rightBounds = curRightBounds;
                bestLeftLength = leftLength;
                bestCost = curTotalCost;
            }

            return (bestCost, leftBounds, rightBounds, bestLeftLength);
        }

        private static Split<TVec, TNum> ChooseSplit<TVec, TNum>(
            AABB<TVec> outer,
            SmartSelectIterator<int, AABB<TVec>> bounds,
            SmartSelectIterator<int, TVec> positions,
            int splitTests)
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            Debug.Assert(bounds.Length == positions.Length);
            var bestSplit = BadSplit<TVec, TNum>();
            if (bounds.Length <= 1)
                return bestSplit;

            for (var test = 0; test < splitTests; test++)
            {
                var splitFactor = TNum.CreateTruncating(test + 1)
                                  / TNum.CreateTruncating(splitTests + 1);
                var splitPos = TVec.Lerp(outer.Min, outer.Max, splitFactor);
                for (var axis = 0; axis < TVec.Dimensions; axis++)
                {
                    var (cost, bbLeft, bbRight) = EvalSplit(bounds, positions, axis, splitPos[axis]);
                    if (!TNum.IsFinite(cost))
                        continue;
                    if (cost >= bestSplit.Cost) continue;
                    bestSplit = new Split<TVec, TNum>(axis, splitPos[axis], cost, bbLeft, bbRight);
                }
            }

            return bestSplit;
        }

        private static Split<TVec, TNum> BadSplit<TVec, TNum>()
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum> =>
            new(-1,
                TNum.NaN,
                TNum.PositiveInfinity,
                AABB<TVec>.Empty,
                AABB<TVec>.Empty);

        private static (TNum cost, AABB<TVec> bbLeft, AABB<TVec> bbRight) EvalSplit<TVec, TNum>
        (SmartSelectIterator<int, AABB<TVec>> bounds,
            SmartSelectIterator<int, TVec> positions,
            int axis,
            TNum splitLevel)
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            Debug.Assert(bounds.Length == positions.Length);
            var n = bounds.Length;
            var boundsLeft = AABB<TVec>.Empty;
            var nLeft = 0;
            var boundsRight = AABB<TVec>.Empty;
            var nRight = 0;
            for (var i = 0; i < n; i++)
            {
                var itemLevel = positions[i][axis];
                var itemBounds = bounds[i];
                var isLeft = splitLevel > itemLevel;
                if (isLeft)
                {
                    boundsLeft = AABB<TVec>.Combine(boundsLeft, itemBounds);
                    ++nLeft;
                }
                else
                {
                    boundsRight = AABB<TVec>.Combine(boundsRight, itemBounds);
                    ++nRight;
                }
            }

            if (nLeft == 0 || nRight == 0) return (TNum.PositiveInfinity, boundsLeft, boundsRight);
            var leftCost = SahLeafCost<TVec, TNum>(boundsLeft.Size, nLeft);
            var rightCost = SahLeafCost<TVec, TNum>(boundsRight.Size, nRight);
            return (leftCost + rightCost, boundsLeft, boundsRight);
        }

        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TNum SahLeafCost<TVec, TNum>(TVec v, int n)
            where TVec : unmanaged, IVecBase<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
            => v.SquaredLength * TNum.CreateTruncating(n);
    }
}