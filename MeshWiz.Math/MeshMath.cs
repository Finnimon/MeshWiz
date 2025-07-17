using System.Numerics;
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
            indices[++indexPosition]=nIndex;
            indices[++indexPosition] = GetIndex(triangle.B, unified, vertices);
            indices[++indexPosition]=nIndex;
            indices[++indexPosition] = GetIndex(triangle.C, unified, vertices);
            indices[++indexPosition]=nIndex;
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


    public  static BoundedVolumeList<TNum> Hierarchize<TNum>(
        TriangleIndexer[] indices, 
        Vector3<TNum>[] vertices,
        int maxDepth=32)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        Vec3Comparer<TNum> comparer=new(vertices);
        BoundedVolumeList<TNum> hierarchy = [new(BBox(vertices), 0, indices.Length)];
        Stack<(int parentIndex,int depth)> recursiveStack = new([(0,0)]);
        while (recursiveStack.TryPop(out var job))
        {
            var (parentIndex,depth) = job;
            if (depth > maxDepth) continue;
            ref var parent = ref hierarchy[parentIndex];
            var parentCost = parent.Cost;
            var (axis, level, cost, bboxLeft, bboxRight) = ChooseSplit(parent, indices, vertices);
            if (cost > parentCost) continue;

            
            comparer.Axis = axis;

            Array.Sort(indices, parent.Start, parent.Length, comparer);
            var leftChildLength = 0;
            for (var i = 0; i < indices.Length; i++)
            {
                var triLevel = indices[i].Extract(vertices).Centroid[axis];
                if (triLevel > level) break;
                leftChildLength++;
            }

            BoundedVolume<TNum> leftChild = new(bboxLeft, parent.Start, leftChildLength);
            BoundedVolume<TNum> rightChild = new(bboxRight, leftChild.End, parent.Length - leftChildLength);
            var leftIndex= (parent.FirstChild = hierarchy.Add(leftChild));
            var rightIndex= (parent.SecondChild = hierarchy.Add(rightChild));
            
            ++depth;
            recursiveStack.Push((leftIndex,depth ));
            recursiveStack.Push((rightIndex,depth ));
        }
        hierarchy.Trim();
        return hierarchy;
    }

    private sealed class Vec3Comparer<TNum> : IComparer<TriangleIndexer>
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        public int Axis;
        public readonly Vector3<TNum>[] Vertices;

        public Vec3Comparer(Vector3<TNum>[] vertices, int axis = 0)
        {
            Axis = axis;
            Vertices = vertices;
        }

        public int Compare(TriangleIndexer left, TriangleIndexer right) 
            => left.Extract(Vertices).Centroid[Axis].CompareTo(right.Extract(Vertices).Centroid[Axis]);
    }


    private static (int bestSplitAxis, TNum bestLevel, TNum bestCost, BBox3<TNum> leftBounds, BBox3<TNum> rightBounds) ChooseSplit<TNum>(
        BoundedVolume<TNum> toSplit,
        TriangleIndexer[] indices, 
        Vector3<TNum>[] vertices,
        uint splitTests=5)
    where TNum:unmanaged,IFloatingPointIeee754<TNum>
    {
        var triCount = toSplit.Length;
        BBox3<TNum> leftBounds=BBox3<TNum>.NegativeInfinity;
        BBox3<TNum> rightBounds=BBox3<TNum>.NegativeInfinity;
        var bestCost = TNum.PositiveInfinity;
        var bestSplitAxis=-1;
        var bestLevel=TNum.NaN;
        if (triCount <= 1) return (bestSplitAxis,bestLevel,bestCost,leftBounds, rightBounds);
        var parentBounds = toSplit.Bounds;
            for (var i = 0; i < splitTests; i++)
            {
                var splitT=TNum.CreateTruncating(i+1) / TNum.CreateTruncating(splitTests + 1);
                var splitPos=Vector3<TNum>.Lerp(parentBounds.Min, parentBounds.Max, splitT);
                for (var axis = 0; axis <= Vector3<TNum>.Dimensions; axis++)
                {
                    var (cost,bbLeft,bbRight)= EvalSplit(toSplit,axis,splitPos[axis],indices,vertices);
                    if(cost>=bestCost) continue;
                    bestCost=cost;
                    bestSplitAxis=axis;
                    bestLevel=splitPos[axis];
                    leftBounds=bbLeft;
                    rightBounds=bbRight;
                }
            }
            return (bestSplitAxis,bestLevel,bestCost,leftBounds,rightBounds);
    }

    private static (TNum cost, BBox3<TNum> boundsLeft, BBox3<TNum> boundsRight) EvalSplit<TNum>(
        BoundedVolume<TNum> parent,
        int axis, 
        TNum splitSuggest,
        TriangleIndexer[] indices, 
        Vector3<TNum>[] vertices)
        where TNum : unmanaged, IFloatingPointIeee754<TNum>
    {
        BBox3<TNum> boundsLeft=BBox3<TNum>.NegativeInfinity;
        BBox3<TNum> boundsRight=BBox3<TNum>.NegativeInfinity;
        var numLeft = 0;
        var numRight = 0;
        for(var i=parent.Start;i<parent.End;i++)
        {
            var tri= indices[i].Extract(vertices);
            var centroid = tri.Centroid;
            var isLeft = centroid[axis] <= splitSuggest;
            if (isLeft)
            {
                boundsLeft=boundsLeft.CombineWith(tri.A).CombineWith(tri.B).CombineWith(tri.C);
                numLeft++;
            }
            else
            {
                boundsRight=boundsRight.CombineWith(tri.A).CombineWith(tri.B).CombineWith(tri.C);
                numRight++;
            }
        }
        
        var leftCost=BoundedVolume<TNum>.NodeCost(boundsLeft.Size, numLeft);
        var rightCost=BoundedVolume<TNum>.NodeCost(boundsRight.Size, numRight);
        return (leftCost + rightCost, boundsLeft, boundsRight);
    }
}