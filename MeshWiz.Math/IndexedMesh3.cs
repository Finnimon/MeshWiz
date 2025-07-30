using System.Collections;
using System.Numerics;

namespace MeshWiz.Math;

public sealed class IndexedMesh3<TNum> : IIndexedMesh3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid => VolumeCentroid;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= MeshMath.VertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= MeshMath.SurfaceCentroid(this).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= MeshMath.VolumeCentroid(this).XYZ;

    public TNum Volume => _volume ??= MeshMath.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= MeshMath.SurfaceArea(this);
    public BBox3<TNum> BBox => _bBox ??= MeshMath.BBox(this);

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;
    private BBox3<TNum>? _bBox;

    public Vector3<TNum>[] Vertices { get; }
    public TriangleIndexer[] Indices { get; }
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);

    public IndexedMesh3(Vector3<TNum>[] vertices, TriangleIndexer[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public IndexedMesh3(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        if (mesh is IndexedMesh3<TNum> indexed)
        {
            Indices = indexed.Indices;
            Vertices = indexed.Vertices;
            return;
        }
        (Indices,Vertices)= MeshMath.Indicate(mesh);
    }

    private static int GetIndex(Vector3<TNum> vec, Dictionary<Vector3<TNum>, int> unified,
        List<Vector3<TNum>> vertices)
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = vertices.Count;
        unified.Add(vec, index);
        vertices.Add(vec);
        return index;
    }


    public void InitializeLazies()
    {
        var info = MeshMath.AllInfo(this);
        _vertexCentroid = info.VertexCentroid;
        _surfaceCentroid = info.SurfaceCentroid;
        _volumeCentroid = info.VolumeCentroid;
        _surfaceArea = info.SurfaceArea;
        _volume = info.Volume;
        _bBox = info.Box;
    }


    public IndexedMesh3<TNum> Indexed()
        => this;


    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public static IndexedMesh3<TNum> Empty { get; } = new([]);

}