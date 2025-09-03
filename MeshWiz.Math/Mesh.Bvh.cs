using System.Numerics;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static partial class Mesh
{
    public static class Bvh
    {
        private readonly struct BvhSortingTriangle<TNum>(Triangle3<TNum> triangle)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            public readonly Triangle3<TNum> Triangle = triangle;
            public readonly AABB<Vector3<TNum>> BBox = triangle.BBox;
            public readonly Vector3<TNum> Centroid = triangle.Centroid;
        }

        private sealed record BvhSortingComparer<TNum> : IComparer<BvhSortingTriangle<TNum>>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            public int Axis;

            public int Compare(BvhSortingTriangle<TNum> x, BvhSortingTriangle<TNum> y)
                => x.Centroid[Axis].CompareTo(y.Centroid[Axis]);
        }

        public static (BoundedVolumeHierarchy<TNum> hierarchy, TriangleIndexer[] indices, Vector3<TNum>[] vertices)
            Hierarchize<TNum>(
                IReadOnlyList<Triangle3<TNum>> mesh,
                uint maxDepth = 32,
                uint splitTests = 4)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            splitTests = uint.Clamp(splitTests, 2, 32);
            var triangles = new BvhSortingTriangle<TNum>[mesh.Count];
            var rootBox = AABB<Vector3<TNum>>.Empty;;
            for (var i = 0; i < mesh.Count; i++)
            {
                var sorting = new BvhSortingTriangle<TNum>(mesh[i]);
                rootBox = rootBox.CombineWith(sorting.BBox);
                triangles[i] = sorting;
            }

            BoundedVolumeHierarchy<TNum> hierarchy = [BoundedVolume<TNum>.MakeLeaf(rootBox, 0, triangles.Length)];

            Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
            recursiveStack.Push((0, 0));
            while (recursiveStack.TryPop(out var job))
            {
                var (parentIndex, depth) = job;
                if (depth > maxDepth) continue;
                ref var parent = ref hierarchy.GetWritable(parentIndex);
                if (parent.Length < 2) continue;

                var (axis, level, cost, bboxLeft, bboxRight) = ChooseSplit(parent, triangles, splitTests);
                if (parent.Cost <= cost) continue;

                var i = parent.Start;
                var j = parent.End - 1;
                while (i <= j)
                {
                    while (i <= j && triangles[i].Centroid[axis] <= level) i++;
                    while (i <= j && triangles[j].Centroid[axis] > level) j--;
                    if (i >= j) continue;
                    (triangles[i], triangles[j]) = (triangles[j], triangles[i]);
                    i++;
                    j--;
                }

                var leftChildLength = i - parent.Start;

                if (leftChildLength.OutsideInclusiveRange(0, parent.Length - 1)) continue;
                BoundedVolume<TNum> leftChild = BoundedVolume<TNum>.MakeLeaf(bboxLeft, parent.Start, leftChildLength);
                BoundedVolume<TNum> rightChild = BoundedVolume<TNum>.MakeLeaf(bboxRight, leftChild.End, parent.Length - leftChildLength);
                var leftIndex = hierarchy.Add(leftChild);
                var rightIndex = hierarchy.Add(rightChild);

                parent.RegisterChildren(leftIndex, rightIndex);

                ++depth;
                recursiveStack.Push((leftIndex, depth));
                recursiveStack.Push((rightIndex, depth));
            }

            hierarchy.Trim();
            var trianglesNaked = new Triangle3<TNum>[triangles.Length];
            for (var i = 0; i < triangles.Length; i++) trianglesNaked[i] = triangles[i].Triangle;
            var (indices, vertices) = Indexing.Indicate(trianglesNaked);
            return (hierarchy, indices, vertices);
        }

        private static (int bestSplitAxis,
            TNum bestLevel,
            TNum bestCost,
            AABB<Vector3<TNum>> leftBounds,
            AABB<Vector3<TNum>> rightBounds)
            ChooseSplit<TNum>(
                in BoundedVolume<TNum> parent,
                BvhSortingTriangle<TNum>[] triangles,
                uint splitTests)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var leftBounds = AABB<Vector3<TNum>>.Empty;
            var rightBounds = AABB<Vector3<TNum>>.Empty;

            var bestCost = TNum.PositiveInfinity;
            var bestSplitAxis = -1;
            var bestLevel = TNum.NaN;

            if (parent.Length <= 1) return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);
            var parentMin = parent.Bounds.Min;
            var parentMax = parent.Bounds.Max;
            var parentStart = parent.Start;
            var parentEnd = parent.End;
            for (var i = 0; i < splitTests; i++)
            {
                var splitFactor = TNum.CreateTruncating(i + 1) / TNum.CreateTruncating(splitTests + 1);
                var splitPos = Vector3<TNum>.Lerp(parentMin, parentMax, splitFactor);
                for (var axis = 0; axis < Vector3<TNum>.Dimensions; axis++)
                {
                    var (cost, bbLeft, bbRight) = EvalSplit(parentStart, parentEnd, axis, splitPos[axis], triangles);
                    if (cost >= bestCost) continue;
                    bestCost = cost;
                    bestSplitAxis = axis;
                    bestLevel = splitPos[axis];
                    leftBounds = bbLeft;
                    rightBounds = bbRight;
                }
            }

            return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);
        }

        private static (TNum cost, AABB<Vector3<TNum>> boundsLeft, AABB<Vector3<TNum>> boundsRight) EvalSplit<TNum>(
            int parentStart,
            int parentEnd,
            int axis,
            TNum splitSuggest,
            BvhSortingTriangle<TNum>[] triangles)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var boundsLeft = AABB<Vector3<TNum>>.Empty;
            var boundsRight = AABB<Vector3<TNum>>.Empty;
            var numLeft = 0;
            var numRight = 0;
            for (var i = parentStart; i < parentEnd; i++)
            {
                ref var sorting = ref triangles[i];
                var isLeft = splitSuggest > (sorting.Centroid[axis]);
                if (isLeft)
                {
                    boundsLeft = boundsLeft.CombineWith(sorting.BBox);
                    numLeft++;
                }
                else
                {
                    boundsRight = boundsRight.CombineWith(sorting.BBox);
                    numRight++;
                }
            }

            if (numLeft == 0 || numRight == 0) return (TNum.PositiveInfinity, boundsLeft, boundsRight);
            var leftCost = BoundedVolume<TNum>.NodeCost(boundsLeft.Size, numLeft);
            var rightCost = BoundedVolume<TNum>.NodeCost(boundsRight.Size, numRight);
            return (leftCost + rightCost, boundsLeft, boundsRight);
        }

        [Obsolete($"Use the other overload. It has no side effects and is much faster.")]
        public static BoundedVolumeHierarchy<TNum> Hierarchize<TNum>(
            TriangleIndexer[] indices,
            Vector3<TNum>[] vertices,
            uint maxDepth = 32,
            uint splitTests = 4)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            splitTests = uint.Clamp(splitTests, 2, 10);
            Vec3Comparer<TNum> comparer = new(vertices);
            BoundedVolumeHierarchy<TNum> hierarchy = [BoundedVolume<TNum>.MakeLeaf(AABB<Vector3<TNum>>.From(vertices), 0, indices.Length)];
            Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
            recursiveStack.Push((0, 0));
            while (recursiveStack.TryPop(out var job))
            {
                var (parentIndex, depth) = job;
                if (depth > maxDepth) continue;
                ref var parent = ref hierarchy.GetWritable(parentIndex);
                if (parent.Length < 2) continue;
                var parentCost = parent.Cost;
                var (axis, level, cost, bboxLeft, bboxRight)
                    = ChooseSplit(parent, indices, vertices, splitTests);
                if (parentCost <= cost) continue;


                comparer.Axis = axis;

                Array.Sort(indices, parent.Start, parent.Length, comparer);
                var leftChildLength = 0;
                var tripleLevel = level * TNum.CreateTruncating(3);
                for (var i = parent.Start; i < parent.End; i++)
                {
                    var indexer = indices[i];
                    var triLevel = vertices[indexer.A][axis] + vertices[indexer.B][axis] + vertices[indexer.C][axis];
                    if (triLevel > tripleLevel) break;
                    leftChildLength++;
                }

                if (leftChildLength.OutsideInclusiveRange(0, parent.Length - 1)) continue;

                BoundedVolume<TNum> leftChild = BoundedVolume<TNum>.MakeLeaf(bboxLeft, parent.Start, leftChildLength);
                BoundedVolume<TNum> rightChild =
                    BoundedVolume<TNum>.MakeLeaf(bboxRight, leftChild.End, parent.Length - leftChildLength);
                var leftIndex = hierarchy.Add(leftChild);
                var rightIndex = hierarchy.Add(rightChild);
                parent.RegisterChildren(leftIndex, rightIndex);

                ++depth;
                recursiveStack.Push((leftIndex, depth));
                recursiveStack.Push((rightIndex, depth));
            }

            hierarchy.Trim();
            return hierarchy;
        }

        [Obsolete]
        private sealed class Vec3Comparer<TNum>(Vector3<TNum>[] vertices, int axis = 0)
            : IComparer<TriangleIndexer>
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            public int Axis = axis;

            public int Compare(TriangleIndexer left, TriangleIndexer right)
                => (vertices[left.A][Axis] + vertices[left.B][Axis] + vertices[left.C][Axis])
                    .CompareTo(vertices[right.A][Axis] + vertices[right.B][Axis] + vertices[right.C][Axis]);
        }

        [Obsolete]
        private static (int bestSplitAxis, TNum bestLevel, TNum bestCost, AABB<Vector3<TNum>> leftBounds, AABB<Vector3<TNum>>
            rightBounds)
            ChooseSplit<TNum>(
                in BoundedVolume<TNum> toSplit,
                TriangleIndexer[] indices,
                Vector3<TNum>[] vertices,
                uint splitTests)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var leftBounds = AABB<Vector3<TNum>>.Empty;
            var rightBounds = AABB<Vector3<TNum>>.Empty;

            var bestCost = TNum.PositiveInfinity;
            var bestSplitAxis = -1;
            var bestLevel = TNum.NaN;

            if (toSplit.Length <= 1) return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);

            var parentMin = toSplit.Bounds.Min;
            var parentMax = toSplit.Bounds.Max;
            var parentStart = toSplit.Start;
            var parentLength = toSplit.Length;
            var centroids = new Vector3<TNum>[parentLength];
            var bounds = new AABB<Vector3<TNum>>[parentLength];
            for (var i = 0; i < parentLength; i++)
            {
                var tri = indices[i + parentStart].Extract(vertices);
                centroids[i] = tri.Centroid;
                bounds[i] = tri.BBox;
            }

            for (var i = 0; i < splitTests; i++)
            {
                var splitT = TNum.CreateTruncating(i + 1) / TNum.CreateTruncating(splitTests + 1);
                var splitPos = Vector3<TNum>.Lerp(parentMin, parentMax, splitT);
                for (var axis = 0; axis < Vector3<TNum>.Dimensions; axis++)
                {
                    var (cost, bbLeft, bbRight) = EvalSplit(axis, splitPos[axis], centroids, bounds);
                    if (cost >= bestCost) continue;
                    bestCost = cost;
                    bestSplitAxis = axis;
                    bestLevel = splitPos[axis];
                    leftBounds = bbLeft;
                    rightBounds = bbRight;
                }
            }

            return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);
        }

        [Obsolete]
        private static (TNum cost, AABB<Vector3<TNum>> boundsLeft, AABB<Vector3<TNum>> boundsRight) EvalSplit<TNum>(
            int axis,
            TNum splitSuggest,
            Vector3<TNum>[] centroids,
            AABB<Vector3<TNum>>[] bounds)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var boundsLeft = AABB<Vector3<TNum>>.Empty;
            var boundsRight = AABB<Vector3<TNum>>.Empty;
            var numLeft = 0;
            var numRight = 0;
            for (var i = 0; i < centroids.Length; i++)
            {
                var isLeft = splitSuggest > (centroids[i][axis]);
                if (isLeft)
                {
                    boundsLeft = boundsLeft.CombineWith(bounds[i]);
                    numLeft++;
                }
                else
                {
                    boundsRight = boundsRight.CombineWith(bounds[i]);
                    numRight++;
                }
            }

            if (numLeft == 0 || numRight == 0) return (TNum.PositiveInfinity, boundsLeft, boundsRight);
            var leftCost = BoundedVolume<TNum>.NodeCost(boundsLeft.Size, numLeft);
            var rightCost = BoundedVolume<TNum>.NodeCost(boundsRight.Size, numRight);
            return (leftCost + rightCost, boundsLeft, boundsRight);
        }

        public static BoundedVolume<TNum>[] RecalculateBBoxes<TNum>(TriangleIndexer[] tris, Vector3<TNum>[] verts,
            BoundedVolume<TNum>[] bounds)
            where TNum : unmanaged, IFloatingPointIeee754<TNum>
        {
            var newBounds = new BoundedVolume<TNum>[bounds.Length];
            for (var i = bounds.Length - 1; i >= 0; i--)
            {
                var bound = bounds[i];
                AABB<Vector3<TNum>> newBBox;
                if (bound.IsParent)
                {
                    newBBox = newBounds[bound.FirstChild].Bounds.CombineWith(newBounds[bound.SecondChild].Bounds);
                    newBounds[i] = BoundedVolume<TNum>.MakeParent(newBBox, bound.FirstChild, bound.SecondChild);
                    continue;
                }

                newBBox = AABB<Vector3<TNum>>.Empty;
                for (var tri = bound.Start; tri < bound.End; tri++)
                {
                    var triBBox = tris[tri].Extract(verts).BBox;
                    newBBox = newBBox.CombineWith(triBBox);
                }

                newBounds[i] = BoundedVolume<TNum>.MakeLeaf(newBBox, bound.Start, bound.Length);
            }

            return newBounds;
        }
    }


}