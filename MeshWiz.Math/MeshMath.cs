using System.Numerics;
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

        foreach (var triangle in mesh)
        {
            var (a,b,c) = triangle;
            var currentCentroid = a + b + c;
            var currentSurf = triangle.SurfaceArea;
            var currentVolume = Tetrahedron<TNum>.CalculateSignedVolume(a,b,c,Vector3<TNum>.Zero);
            vertexCentroid += currentCentroid;
            surfaceCentroid += currentCentroid * currentSurf;
            volumeCentroid += currentCentroid * currentVolume;
            volume += currentVolume;
            surfaceArea += currentSurf;
            box= box.CombineWith(triangle.A).CombineWith(triangle.B).CombineWith(triangle.C);
        }

        vertexCentroid /= TNum.CreateTruncating(mesh.Count*3);
        surfaceCentroid /= surfaceArea * TNum.CreateTruncating(3);
        volumeCentroid /= volume*TNum.CreateTruncating(4);
        volume = TNum.Abs(volume);
        
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
        foreach (var tri in mesh) centroid += tri.A + tri.B + tri.C;

        return centroid / TNum.CreateTruncating(mesh.Count * 3);
    }

    public static Vector4<TNum> SurfaceCentroid<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var centroid = Vector4<TNum>.Zero;
        foreach (var triangle in mesh)
        {
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
        foreach (var t in mesh)
        {
            Tetrahedron<TNum> tetra = new(t);
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
        foreach (var tri in mesh)
            volume += new Tetrahedron<TNum>(tri).Volume;

        return volume;
    }


    public static TNum SurfaceArea<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var area = TNum.Zero;
        foreach (var tri in mesh) area += tri.SurfaceArea;
        return area;
    }

    public static BBox3<TNum> BBox<TNum>(IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        foreach (var tri in mesh) 
            bbox = bbox.CombineWith(tri.A).CombineWith(tri.B).CombineWith(tri.C);

        return bbox;
    }

    public static BBox3<TNum> BBox<TNum>(IReadOnlyList<Vector3<TNum>> vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        foreach (var tri in vertices)
            bbox = bbox.CombineWith(tri);

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
        var unified = new Dictionary<Vector3<TNum>, int>(averageUniqueVertices);

        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            var aIndex = IndexerUtilities.GetIndex(triangle.A, unified, vertices);
            var bIndex = IndexerUtilities.GetIndex(triangle.B, unified, vertices);
            var cIndex = IndexerUtilities.GetIndex(triangle.C, unified, vertices);
            indices[i] = new TriangleIndexer(aIndex, bIndex, cIndex);
        }

        return (indices, [..vertices]);
    }

    public static (int[] Indices, Vector3<TNum>[] Vertices) IndicateWithNormals<TNum>(
        IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var indices = new int[mesh.Count * 4];
        //on avg there is two triangles per unique vertex
        var averageUniqueVertices = mesh.Count / 2;
        var vertices = new List<Vector3<TNum>>(averageUniqueVertices);
        var unified = new Dictionary<Vector3<TNum>, int>(averageUniqueVertices);
        var indexPosition = -1;
        foreach (var triangle in mesh)
        {
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.A, unified, vertices);
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.B, unified, vertices);
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.C, unified, vertices);
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.Normal, unified, vertices);
        }

        return (indices, [..vertices]);
    }

    public static (int[] Indices, Vector3<TNum>[] Vertices) IndicateWithNormalsInterleaved<TNum>(
        IReadOnlyList<Triangle3<TNum>> mesh)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var indices = new int[mesh.Count * 6];
        //on avg there is two triangles per unique vertex
        var averageUniqueVertices = mesh.Count / 2;
        var vertices = new List<Vector3<TNum>>(averageUniqueVertices);
        var unified = new Dictionary<Vector3<TNum>, int>(averageUniqueVertices);
        var indexPosition = -1;
        foreach (var triangle in mesh)
        {
            var nIndex = IndexerUtilities.GetIndex(triangle.Normal, unified, vertices);
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.A, unified, vertices);
            indices[++indexPosition] = nIndex;
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.B, unified, vertices);
            indices[++indexPosition] = nIndex;
            indices[++indexPosition] = IndexerUtilities.GetIndex(triangle.C, unified, vertices);
            indices[++indexPosition] = nIndex;
        }

        return (indices, [..vertices]);
    }


    private readonly struct BvhSortingTriangle<TNum>(Triangle3<TNum> triangle)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public readonly Triangle3<TNum> Triangle = triangle;
        public readonly BBox3<TNum> BBox = triangle.BBox;
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
        var rootBox = BBox3<TNum>.NegativeInfinity;
        for (var i = 0; i < mesh.Count; i++)
        {
            var sorting= new BvhSortingTriangle<TNum>(mesh[i]);
            rootBox = rootBox.CombineWith(sorting.BBox);
            triangles[i] = sorting;
        }
        var comparer=new BvhSortingComparer<TNum>();
        BoundedVolumeHierarchy<TNum> hierarchy = [BoundedVolume<TNum>.MakeLeaf(rootBox,0,triangles.Length)];
        
        Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
        recursiveStack.Push((0, 0));
        while (recursiveStack.TryPop(out var job))
        {
            var (parentIndex, depth) = job;
            if (depth > maxDepth) continue;
            ref var parent = ref hierarchy.GetWritable(parentIndex);
            if(parent.Length<2) continue;
            
            var (axis, level, cost, bboxLeft, bboxRight) = ChooseSplit(parent, triangles, splitTests);
            if (parent.Cost<=cost) continue;
            
            comparer.Axis = axis;
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

            if (leftChildLength.OutsideInclusiveRange(0,parent.Length-1)) continue;
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
        for (var i = 0; i < triangles.Length; i++)trianglesNaked[i]=triangles[i].Triangle;
        var (indices, vertices) = Indicate(trianglesNaked);
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

    [Obsolete($"Use the other overload. It has no side effects and is much faster.")]
    public static BoundedVolumeHierarchy<TNum> Hierarchize<TNum>(
        TriangleIndexer[] indices,
        Vector3<TNum>[] vertices,
        uint maxDepth = 32,
        uint splitTests=4)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        splitTests = uint.Clamp(splitTests, 2, 10);
        Vec3Comparer<TNum> comparer = new(vertices);
        BoundedVolumeHierarchy<TNum> hierarchy = [BoundedVolume<TNum>.MakeLeaf(BBox(vertices),0,indices.Length)];
        Stack<(int parentIndex, uint depth)> recursiveStack = new((int)maxDepth);
        recursiveStack.Push((0, 0));
        while (recursiveStack.TryPop(out var job))
        {
            var (parentIndex, depth) = job;
            if (depth > maxDepth) continue;
            ref var parent = ref hierarchy.GetWritable(parentIndex);
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

            BoundedVolume<TNum> leftChild = BoundedVolume<TNum>.MakeLeaf(bboxLeft, parent.Start, leftChildLength);
            BoundedVolume<TNum> rightChild = BoundedVolume<TNum>.MakeLeaf(bboxRight, leftChild.End, parent.Length - leftChildLength);
            var leftIndex =  hierarchy.Add(leftChild);
            var rightIndex =  hierarchy.Add(rightChild);
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

    [Obsolete]
    private static (TNum cost, BBox3<TNum> boundsLeft, BBox3<TNum> boundsRight) EvalSplit<TNum>(
        int axis,
        TNum splitSuggest,
        Vector3<TNum>[] centroids,
        BBox3<TNum>[] bounds)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        var boundsLeft = BBox3<TNum>.NegativeInfinity;
        var boundsRight = BBox3<TNum>.NegativeInfinity;
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