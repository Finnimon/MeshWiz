using System;
using System.Collections.Generic;
using System.Numerics;
using MeshWiz.RefLinq;
using MeshWiz.Utility.Extensions;
using BvhStep = (int ParentIndex, int Depth);

namespace MeshWiz.Math;

public static partial class Bvh
{
    public static partial class Create
    {
        // public static Info<TVec, TNum> LongestAxisSplit<TBounded, TVec, TNum>(
        //     IReadOnlyList<TBounded> source,
        //     int maxDepth = 32,
        //     int minNodeSize = 1
        // )
        //     where TBounded : IBounded<TVec>, IPosition<TBounded, TVec, TNum>
        //     where TVec : unmanaged, IVec<TVec, TNum>
        //     where TNum : unmanaged, IFloatingPointIeee754<TNum>
        // {
        //     minNodeSize = int.Max(1, minNodeSize);
        //     var perItemBounds = source.Select(item => item.BBox).ToArray();
        //     var n = perItemBounds.Length;
        //     var indexShuffle = Enumerable.Range(0, n).ToArray();
        //     var shuffledIndexSpan = indexShuffle.AsSpan();
        //     var shuffledBounds = shuffledIndexSpan
        //         .Select(i => perItemBounds[i]);
        //     var shuffledPositions = shuffledIndexSpan
        //         .Select(i => source[i].Position);
        //     var rootBox = AABB.Combine(perItemBounds);
        //     List<Node<TVec, TNum>> nodes = [Node<TVec, TNum>.MakeLeaf(rootBox, 0, n)];
        //     Stack<BvhStep> recursion = new(maxDepth * 2);
        //     recursion.Push((0, 1));
        //     var currentMaxDepth = 1;
        //     while (recursion.TryPop(out var step))
        //     {
        //         var (parentIndex, depth) = step;
        //         var parent = nodes[parentIndex];
        //
        //         if (parent.Length <= minNodeSize)
        //             continue;
        //
        //         var size = parent.Bounds.Size;
        //         var axis = 0;
        //         for (var dim = 1; dim < TVec.Dimensions; dim++)
        //         {
        //             if (size[dim] < size[axis]) continue;
        //             axis = dim;
        //         }
        //
        //         var level = size[axis];
        //
        //         var i = parent.Start;
        //         var j = parent.End - 1;
        //         while (i <= j)
        //         {
        //             while (i <= j && shuffledPositions[i][axis] <= level) i++;
        //             while (i <= j && shuffledPositions[j][axis] > level) j--;
        //             if (i >= j) continue;
        //             shuffledIndexSpan.Swap(i, j);
        //             i++;
        //             j--;
        //         }
        //
        //         var leftChildLength = i - parent.Start;
        //
        //         if (leftChildLength.OutsideInclusiveRange(0, parent.Length - 1)) continue;
        //         var bboxLeft = shuffledBounds
        //             .Take(parent.Start..(parent.Start + leftChildLength))
        //             .Aggregate(AABB<TVec>.Combine);
        //         var leftChild = Node<TVec, TNum>.MakeLeaf(bboxLeft, parent.Start, leftChildLength);
        //         var bboxRight = shuffledBounds
        //             .Take(leftChild.End..parent.End)
        //             .Aggregate(AABB<TVec>.Combine);
        //
        //
        //         var rightChild = Node<TVec, TNum>.MakeLeaf(bboxRight, leftChild.End, parent.Length - leftChildLength);
        //         var leftIndex = nodes.Count;
        //         nodes.Add(leftChild);
        //         var rightIndex = nodes.Count;
        //         nodes.Add(rightChild);
        //
        //         nodes[parentIndex] = parent.WithChildren(leftIndex, rightIndex);
        //         ++depth;
        //         currentMaxDepth = int.Max(depth, currentMaxDepth);
        //         if(depth>=maxDepth) continue;
        //         recursion.Push((leftIndex, depth));
        //         recursion.Push((rightIndex, depth));
        //     }
        //
        //     return new Info<TVec, TNum>(nodes.ToArray(), indexShuffle, currentMaxDepth);
        // }

        /// <summary>
        /// Best use case are polylines
        /// </summary>
        public static Info<TVec, TNum> LinearNonReordering<TBounded, TVec, TNum>(
            IReadOnlyList<TBounded> source,
            int maxDepth = 32,
            int minNodeSize = 1
        )
            where TBounded : IBounded<TVec>
            where TVec : unmanaged, IVec<TVec, TNum>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            minNodeSize = int.Max(1, minNodeSize);
            var perItemBounds = source.Iterate()
                .Select(item => item.BBox)
                .ToArray();
            var boundsSpan = perItemBounds.AsSpan();

            var n = perItemBounds.Length;
            var rootBox = AABB.Combine(perItemBounds);
            List<Node<TVec, TNum>> nodes = [Node<TVec, TNum>.MakeLeaf(rootBox, 0, n)];
            Stack<BvhStep> recursion = new(maxDepth * 2);
            recursion.Push((0, 1));
            var resultDepth = 1;
            while (recursion.TryPop(out var step))
            {
                var (parentIndex, depth) = step;
                var parent = nodes[parentIndex];

                if (parent.Length <= minNodeSize)
                    continue;
                var curBounds = boundsSpan[parent.Start..parent.End];
                var leftEnd =
                    parent.Length / 2 + parent.Length % 2;
                var bbLeft = curBounds[..leftEnd].Iterate().Aggregate(AABB<TVec>.Combine);
                var bbRight = curBounds[leftEnd..].Iterate().Aggregate(AABB<TVec>.Combine);

                var leftChildLength = leftEnd - parent.Start;

                var leftChild = Node<TVec, TNum>.MakeLeaf(bbLeft, parent.Start, leftChildLength);
                var rightChild = Node<TVec, TNum>.MakeLeaf(bbRight, leftEnd, parent.Length - leftChildLength);
                if(leftChild.LeafCost+rightChild.LeafCost>=parent.LeafCost)
                    continue;
                var leftIndex = nodes.Count;
                nodes.Add(leftChild);
                var rightIndex = leftIndex + 1;
                nodes.Add(rightChild);
                nodes[parentIndex] = parent.WithChildren(leftIndex, rightIndex);
                ++depth;
                resultDepth = int.Max(depth, resultDepth);
                if(depth>=maxDepth)
                    continue;
                recursion.Push((rightIndex, depth));
                recursion.Push((leftIndex, depth)); //visit left first
            }

            return new Info<TVec, TNum>(nodes.ToArray(), null, resultDepth);
        }
    }
}