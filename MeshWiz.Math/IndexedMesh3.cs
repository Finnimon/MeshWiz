using System.Collections;
using System.Numerics;

namespace MeshWiz.Math;

public class IndexedMesh3<TNum> : IMesh3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid => VolumeCentroid;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= CalculateVertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= CalculateAreaCentroid(this);
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= CalculateVolumeCentroid(this);

    public TNum Volume => _volume ??= CalculateVolume(this);
    Vector3<TNum> IFace<Vector3<TNum>, TNum>.Centroid => SurfaceCentroid;
    public TNum SurfaceArea => _surfaceArea ??= CalculateSurfaceArea(this);
    public BBox3<TNum> BBox => _bBox ??= GetBBox(this);

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;
    private BBox3<TNum>? _bBox;

    public readonly Vector3<TNum>[] Vertices;
    public readonly TriangleIndexer[] Indices;
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);

    public IndexedMesh3(Vector3<TNum>[] vertices, TriangleIndexer[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public IndexedMesh3(IReadOnlyList<Triangle3<TNum>> triangles)
    {
        if (triangles is IndexedMesh3<TNum> mesh)
        {
            this.Indices = mesh.Indices;
            this.Vertices = mesh.Vertices;
            return;
        }
        Indices = new TriangleIndexer[triangles.Count];
        var vertices=new List<Vector3<TNum>>(triangles.Count);
        var unified=new Dictionary<Vector3<TNum>, int>(vertices.Count);
        
        for (var i = 0; i < triangles.Count; i++)
        {
            var (a, b, c) = triangles[i];
            var aIndex = GetIndex(a, unified, vertices);
            var bIndex = GetIndex(b, unified, vertices);
            var cIndex = GetIndex(c, unified, vertices);
            Indices[i]=new TriangleIndexer(aIndex,bIndex,cIndex);
        }

        Vertices = vertices.ToArray();
    }

    private static int GetIndex(in Vector3<TNum> vec, Dictionary<Vector3<TNum>,int> unified, List<Vector3<TNum>> vertices)
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = vertices.Count;
        unified.Add(vec, index);
        vertices.Add(vec);
        return index;
    }

    public void InitializeLazies()
    {
        var vertexCentroid = Vector3<TNum>.Zero;
        var surfaceCentroid = Vector3<TNum>.Zero;
        var volumeCentroid = Vector3<TNum>.Zero;
        var surfaceArea = TNum.Zero;
        var volume = TNum.Zero;
        var box = BBox3<TNum>.NegativeInfinity;

        for (var i = 0; i < Count; i++)
        {
            var triangle = this[i];
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

        _vertexCentroid = vertexCentroid / TNum.CreateTruncating(Count);
        _surfaceCentroid = surfaceCentroid / surfaceArea;
        _volumeCentroid = volumeCentroid / volume;
        _surfaceArea = surfaceArea;
        _volume = volume;
        _bBox = box;
    }


    public IndexedMesh3<TNum> Indexed()
        => this;

    public IEnumerator<Triangle3<TNum>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public static Vector3<TNum> CalculateVertexCentroid(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            centroid += triangle.A + triangle.B + triangle.C;
        }

        return centroid / TNum.CreateTruncating(mesh.Count * 3);
    }

    public static Vector3<TNum> CalculateAreaCentroid(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        var area = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            var currentCentroid = triangle.A + triangle.B + triangle.C;
            var currentArea = triangle.SurfaceArea;
            centroid += currentCentroid * currentArea;
            area += currentArea;
        }

        return centroid / area / TNum.CreateTruncating(3);
    }


    public static Vector3<TNum> CalculateVolumeCentroid(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var centroid = Vector3<TNum>.Zero;
        var volume = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++)
        {
            Tetrahedron<TNum> tetra = new(mesh[i]);
            var currentVolume = tetra.Volume;
            var currentCentroid = tetra.Centroid;
            centroid += currentCentroid * currentVolume;
            volume += currentVolume;
        }

        return centroid / volume;
    }


    public static TNum CalculateVolume(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var volume = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++) volume += new Tetrahedron<TNum>(mesh[i]).Volume;
        return volume;
    }


    public static TNum CalculateSurfaceArea(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var area = TNum.Zero;
        for (var i = 0; i < mesh.Count; i++) area += mesh[i].SurfaceArea;
        return area;
    }

    public static BBox3<TNum> GetBBox(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        var bbox = BBox3<TNum>.NegativeInfinity;
        for (var i = 1; i < mesh.Count; i++)
        {
            var triangle = mesh[i];
            bbox = bbox.CombineWith(triangle.A).CombineWith(triangle.B).CombineWith(triangle.C);
        }

        return bbox;
    }

    public static IndexedMesh3<TNum> Empty { get; } = new([]);
}