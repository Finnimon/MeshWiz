using System.Numerics;
using System.Runtime.CompilerServices;
using MeshWiz.Math;
using MeshWiz.Utility.Extensions;

namespace MeshWiz.Math;

public static class MeshMath
{
    public record Mesh3Info<TNum>(
        Vector3<TNum> VertexCentroid,
        Vector3<TNum> SurfaceCentroid,
        Vector3<TNum> VolumeCentroid,
        TNum SurfaceArea,
        TNum Volume,
        BBox3<TNum> Box)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>;

    public static Mesh3Info<TNum> AllInfo<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var vertexCentroid = Vector3<TNum>.Zero;
        var surfaceCentroid = Vector3<TNum>.Zero;
        var volumeCentroid = Vector3<TNum>.Zero;
        var surfaceArea = TNum.Zero;
        var volume = TNum.Zero;
        var box = BBox3<TNum>.NegativeInfinity;

        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            Tetrahedron<TNum> tetra = new(in triangle);
            var currentCentroid = triangle.Centroid;
            var currentSurf = triangle.SurfaceArea;
            var currentVolu = tetra.Volume;
            vertexCentroid += currentCentroid;
            surfaceCentroid += currentCentroid * currentSurf;
            volumeCentroid += tetra.Centroid * currentVolu;
            volume += tetra.Volume;
            surfaceArea += currentSurf;
            box.CombineWith(triangle.A).CombineWith(triangle.B).CombineWith(triangle.C);
        }

        vertexCentroid /= TNum.CreateTruncating(mesh.Count);
        surfaceCentroid /= surfaceArea;
        volumeCentroid /= volume;

        return new Mesh3Info<TNum>(
            vertexCentroid,
            surfaceCentroid,
            volumeCentroid,
            surfaceArea,
            volume,
            box
        );
    }

    public static Vector3<TNum> VertexCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var centroid = Vector3<TNum>.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            var tri = mesh[i];
            centroid += tri.A + tri.B + tri.C;
        }

        return centroid / TNum.CreateTruncating(mesh.Count * 3);
    }

    public static Vector4<TNum> SurfaceCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var centroid = Vector4<TNum>.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            var currentCentroid = triangle.A + triangle.B + triangle.C;
            var currentArea = triangle.SurfaceArea;
            centroid += new Vector4<TNum>(currentCentroid * currentArea, currentArea);
        }

        return new Vector4<TNum>(
            centroid.XYZ / centroid.W / TNum.CreateTruncating(3),
            TNum.Abs(centroid.W));
    }

    public static Vector4<TNum> VolumeCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var centroid = Vector4<TNum>.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            Tetrahedron<TNum> tetra = new(mesh[i]);
            var currentVolume = tetra.Volume;
            var currentCentroid = tetra.Centroid;
            centroid += new Vector4<TNum>(currentCentroid * currentVolume, currentVolume);
        }

        return new Vector4<TNum>(
            centroid.XYZ / centroid.W,
            TNum.Abs(centroid.W));
    }

    public static TNum Volume<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var volume = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++) volume += new Tetrahedron<TNum>(mesh[i]).Volume;
        return volume;
    }


    public static TNum SurfaceArea<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var area = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++) area += mesh[i].SurfaceArea;
        return area;
    }

    public static BBox3<TNum> BBox<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        for (var i = 0; i < mesh.Count; i++)
        {
            var tri = mesh[i];
            bbox = bbox.CombineWith(tri.A).CombineWith(tri.B).CombineWith(tri.C);
        }

        return bbox;
    }

    public static BBox3<TNum> BBox<TNum>(IReadOnlyList<Vector3<TNum>> vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        for (var i = 0; i < vertices.Count; i++)
            bbox = bbox.CombineWith(vertices[i]);
        return bbox;
    }

    public static (TriangleIndexer[] Indices, Vector3<TNum>[] Vertices) Indicate<TNum>(
        IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var indices = new TriangleIndexer[mesh.Count];
        //on avg there is two triangles per unique vertex
        var averageUniqueVertices = mesh.Count / 2;
        var vertices = new List<Vector3<TNum>>(averageUniqueVertices);
        var unified = new Dictionary<Vector3<TNum>, uint>(averageUniqueVertices);

        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            var aIndex = GetIndex(triangle.A, unified, vertices);
            var bIndex = GetIndex(triangle.B, unified, vertices);
            var cIndex = GetIndex(triangle.C, unified, vertices);
            indices[i] = new TriangleIndexer(aIndex, bIndex, cIndex);
        }

        return (indices, [..vertices]);
    }

    public static (uint[] Indices, Vector3<TNum>[] Vertices) IndicateWithNormals<TNum>(
        IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var indices = new uint[mesh.Count * 4];
        //on avg there is two triangles per unique vertex
        var averageUniqueVertices = mesh.Count / 2;
        var vertices = new List<Vector3<TNum>>(averageUniqueVertices);
        var unified = new Dictionary<Vector3<TNum>, uint>(averageUniqueVertices);
        var indexPosition = -1;
        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            indices[++indexPosition] = GetIndex(triangle.A, unified, vertices);
            indices[++indexPosition] = GetIndex(triangle.B, unified, vertices);
            indices[++indexPosition] = GetIndex(triangle.C, unified, vertices);
            indices[++indexPosition] = GetIndex(triangle.Normal, unified, vertices);
        }

        return (indices, [..vertices]);
    }

    public static (uint[] Indices, Vector3<TNum>[] Vertices) IndicateWithNormalsInterleaved<TNum>(
        IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var indices = new uint[mesh.Count * 6];
        //on avg there is two triangles per unique vertex
        var averageUniqueVertices = mesh.Count / 2;
        var vertices = new List<Vector3<TNum>>(averageUniqueVertices);
        var unified = new Dictionary<Vector3<TNum>, uint>(averageUniqueVertices);
        var indexPosition = -1;
        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            var nIndex = GetIndex(triangle.Normal, unified, vertices);
            indices[++indexPosition] = GetIndex(triangle.A, unified, vertices);
            indices[++indexPosition] = nIndex;
            indices[++indexPosition] = GetIndex(triangle.B, unified, vertices);
            indices[++indexPosition] = nIndex;
            indices[++indexPosition] = GetIndex(triangle.C, unified, vertices);
            indices[++indexPosition] = nIndex;
        }

        return (indices, [..vertices]);
    }

    private static uint GetIndex<TNum>(Vector3<TNum> vec,
        Dictionary<Vector3<TNum>, uint> unified,
        List<Vector3<TNum>> vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = uint.CreateChecked(vertices.Count);
        unified.Add(vec, index);
        vertices.Add(vec);
        return index;
    }

    private readonly struct BvhSortingTriangle<TNum>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public readonly Triangle3<TNum> Triangle;
        public readonly BBox3<TNum> BBox;
        public readonly Vector3<TNum> Centroid;

        public BvhSortingTriangle(Triangle3<TNum> triangle)
        {
            Triangle=triangle;
            BBox=triangle.BBox;
            Centroid=triangle.Centroid;
        }
    }

    private sealed record BvhSortingComparer<TNum> : IComparer<BvhSortingTriangle<TNum>>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public int Axis = 0;
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
        var rootBox = BBox3<TNum>.NegativeInfinity;
        for (var i = 0; i < mesh.Count; i++)
        {
            var sorting= new BvhSortingTriangle<TNum>(mesh[i]);
            rootBox = rootBox.CombineWith(sorting.BBox);
            triangles[i] = sorting;
        }
        var comparer=new BvhSortingComparer<TNum>();
        BoundedVolume<TNum> rootParent = new(rootBox, 0, triangles.Length);
        BoundedVolumeHierarchy<TNum> hierarchy = [rootParent];
        Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
        recursiveStack.Push((0, 0));
        while (recursiveStack.TryPop(out var job))
        {
            var (parentIndex, depth) = job;
            if (depth > maxDepth) continue;
            ref var parent = ref hierarchy[parentIndex];
            if(parent.Length<2) continue;
            
            var (axis, level, cost, bboxLeft, bboxRight) = ChooseSplit(parent, triangles, splitTests);
            if (parent.Cost<=cost) continue;
            
            comparer.Axis = axis;
            Array.Sort(triangles,parent.Start,parent.Length,comparer);
            var leftChildLength = 0;
            for (var i = parent.Start; i < parent.End; i++)
            {
                if (triangles[i].Centroid[axis] > level) break;
                leftChildLength++;
            }
            if (leftChildLength.OutsideInclusiveRange(0,parent.Length-1)) continue;
            BoundedVolume<TNum> leftChild = new(bboxLeft, parent.Start, leftChildLength);
            BoundedVolume<TNum> rightChild = new(bboxRight, leftChild.End, parent.Length - leftChildLength);
            var leftIndex = (parent.FirstChild = hierarchy.Add(leftChild));
            var rightIndex = (parent.SecondChild = hierarchy.Add(rightChild));

            ++depth;
            recursiveStack.Push((leftIndex, depth));
            recursiveStack.Push((rightIndex, depth));
        }
        
        hierarchy.Trim();
        var trianglesNaked = new Triangle3<TNum>[triangles.Length];
        for (var i = 0; i < triangles.Length; i++)trianglesNaked[i]=triangles[i].Triangle;
        var (indices, vertices) = Indicate<TNum>(trianglesNaked);
        return (hierarchy, indices, vertices);

    }

    private static (int bestSplitAxis,
        TNum bestLevel,
        TNum bestCost,
        BBox3<TNum> leftBounds,
        BBox3<TNum> rightBounds)
        ChooseSplit<TNum>(
            in BoundedVolume<TNum> parent, 
            BvhSortingTriangle<TNum>[] triangles,
            uint splitTests)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var leftBounds = BBox3<TNum>.NegativeInfinity;
        var rightBounds = BBox3<TNum>.NegativeInfinity;
        
        var bestCost = TNum.PositiveInfinity;
        var bestSplitAxis = -1;
        var bestLevel = TNum.NaN;
        
        if (parent.Length <= 1) return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);
        var parentMin = parent.Bounds.Min;
        var parentMax=parent.Bounds.Max;
        var parentStart=parent.Start;
        var parentEnd = parent.End;
        for (var i = 0; i < splitTests; i++)
        {
            var splitFactor = TNum.CreateTruncating(i + 1) / TNum.CreateTruncating(splitTests + 1);
            var splitPos = Vector3<TNum>.Lerp(parentMin, parentMax, splitFactor);
            for (var axis = 0; axis < Vector3<TNum>.Dimensions; axis++)
            {
                var (cost, bbLeft, bbRight) = EvalSplit(parentStart,parentEnd,axis, splitPos[axis], triangles);
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

    private static (TNum cost, BBox3<TNum> boundsLeft, BBox3<TNum> boundsRight) EvalSplit<TNum>(
        int parentStart, 
        int parentEnd,
        int axis,
        TNum splitSuggest,
        BvhSortingTriangle<TNum>[] triangles)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var boundsLeft = BBox3<TNum>.NegativeInfinity;
        var boundsRight = BBox3<TNum>.NegativeInfinity;
        var numLeft = 0;
        var numRight = 0;
        for (var i = parentStart; i < parentEnd; i++)
        {
            ref var sorting = ref triangles[i];
            var isLeft = splitSuggest>(sorting.Centroid[axis]);
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

    [Obsolete($"Use the other overload. It has no sideeffects and is much faster.")]
    public static BoundedVolumeHierarchy<TNum> Hierarchize<TNum>(
        TriangleIndexer[] indices,
        Vector3<TNum>[] vertices,
        uint maxDepth = 32,
        uint splitTests=4)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        splitTests = uint.Clamp(splitTests, 2, 10);
        Vec3Comparer<TNum> comparer = new(vertices);
        BoundedVolumeHierarchy<TNum> hierarchy = [new(BBox(vertices), 0, indices.Length)];
        Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
        recursiveStack.Push((0, 0));
        while (recursiveStack.TryPop(out var job))
        {
            var (parentIndex, depth) = job;
            if (depth > maxDepth) continue;
            ref var parent = ref hierarchy[parentIndex];
            if(parent.Length<2)  continue;
            var parentCost = parent.Cost;
            var (axis, level, cost, bboxLeft, bboxRight) 
                = ChooseSplit(parent, indices, vertices,splitTests);
            if (parentCost <= cost) continue;


            comparer.Axis = axis;

            Array.Sort(indices, parent.Start, parent.Length, comparer);
            var leftChildLength = 0;
            var tripleLevel = level * TNum.CreateTruncating(3);
            for (var i = parent.Start; i < parent.End; i++)
            {
                var indexer=indices[i];
                var triLevel = vertices[indexer.A][axis] + vertices[indexer.B][axis] + vertices[indexer.C][axis];
                if (triLevel > tripleLevel) break;
                leftChildLength++;
            }

            if (leftChildLength.OutsideInclusiveRange(0,parent.Length-1)) continue;

            BoundedVolume<TNum> leftChild = new(bboxLeft, parent.Start, leftChildLength);
            BoundedVolume<TNum> rightChild = new(bboxRight, leftChild.End, parent.Length - leftChildLength);
            var leftIndex = (parent.FirstChild = hierarchy.Add(leftChild));
            var rightIndex = (parent.SecondChild = hierarchy.Add(rightChild));

            ++depth;
            recursiveStack.Push((leftIndex, depth));
            recursiveStack.Push((rightIndex, depth));
        }

        hierarchy.Trim();
        return hierarchy;
    }

    private sealed class Vec3Comparer<TNum>(Vector3<TNum>[] vertices, int axis = 0)
        : IComparer<TriangleIndexer>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public int Axis = axis;
        public readonly Vector3<TNum>[] Vertices=vertices;
        private static readonly TNum Third = TNum.CreateChecked(1.0 / 3.0);

        public int Compare(TriangleIndexer left, TriangleIndexer right) => (Vertices[left.A][Axis] + Vertices[left.B][Axis] + Vertices[left.C][Axis]).CompareTo(Vertices[right.A][Axis] + Vertices[right.B][Axis] + Vertices[right.C][Axis]);
    }

    private static (int bestSplitAxis, TNum bestLevel, TNum bestCost, BBox3<TNum> leftBounds, BBox3<TNum> rightBounds)
        ChooseSplit<TNum>(
            in BoundedVolume<TNum> toSplit,
            TriangleIndexer[] indices,
            Vector3<TNum>[] vertices,
            uint splitTests)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var leftBounds = BBox3<TNum>.NegativeInfinity;
        var rightBounds = BBox3<TNum>.NegativeInfinity;
        
        var bestCost = TNum.PositiveInfinity;
        var bestSplitAxis = -1;
        var bestLevel = TNum.NaN;
        
        if (toSplit.Length <= 1) return (bestSplitAxis, bestLevel, bestCost, leftBounds, rightBounds);
        
        var parentMin = toSplit.Bounds.Min;
        var parentMax=toSplit.Bounds.Max;
        var parentStart=toSplit.Start;
        var parentLength = toSplit.Length;
        var centroids=new Vector3<TNum>[parentLength];
        var bounds=new BBox3<TNum>[parentLength];
        for(var i=0; i<parentLength; i++)
        {
            var tri = indices[i+parentStart].Extract(vertices);
            centroids[i] = tri.Centroid;
            bounds[i] = tri.BBox;
        }
        
        for (var i = 0; i < splitTests; i++)
        {
            var splitT = TNum.CreateTruncating(i + 1) / TNum.CreateTruncating(splitTests + 1);
            var splitPos = Vector3<TNum>.Lerp(parentMin, parentMax, splitT);
            for (var axis = 0; axis < Vector3<TNum>.Dimensions; axis++)
            {
                var (cost, bbLeft, bbRight) = EvalSplit(axis, splitPos[axis], centroids,bounds);
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

    private static (TNum cost, BBox3<TNum> boundsLeft, BBox3<TNum> boundsRight) EvalSplit<TNum>(
        int axis,
        TNum splitSuggest,
        Vector3<TNum>[] centroids,
        BBox3<TNum>[] bounds)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        BBox3<TNum> boundsLeft = BBox3<TNum>.NegativeInfinity;
        BBox3<TNum> boundsRight = BBox3<TNum>.NegativeInfinity;
        var numLeft = 0;
        var numRight = 0;
        for (var i = 0; i < centroids.Length; i++)
        {
            var isLeft = splitSuggest>(centroids[i][axis]);
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
}