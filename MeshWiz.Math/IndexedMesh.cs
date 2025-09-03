using System.Numerics;

namespace MeshWiz.Math;

public sealed class IndexedMesh<TNum> : IIndexedMesh<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> Centroid => VolumeCentroid;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= Mesh.Math.VertexCentroid(this);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= Mesh.Math.VolumeCentroid(this).XYZ;

    public TNum Volume => _volume ??= Mesh.Math.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);
    public AABB<Vector3<TNum>> BBox => _bBox ??= AABB<Vector3<TNum>>.From(Vertices);

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;
    private AABB<Vector3<TNum>>? _bBox;

    public Vector3<TNum>[] Vertices { get; }
    public TriangleIndexer[] Indices { get; }
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);

    public IndexedMesh(Vector3<TNum>[] vertices, TriangleIndexer[] indices)
    {
        Vertices = vertices;
        Indices = indices;
    }

    public IndexedMesh(IReadOnlyList<Triangle3<TNum>> mesh)
    {
        if (mesh is IndexedMesh<TNum> indexed)
        {
            Indices = indexed.Indices;
            Vertices = indexed.Vertices;
            return;
        }
        (Indices,Vertices)= Mesh.Indexing.Indicate(mesh);
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
        var info = Mesh.Math.AllInfo(this);
        _vertexCentroid = info.VertexCentroid;
        _surfaceCentroid = info.SurfaceCentroid;
        _volumeCentroid = info.VolumeCentroid;
        _surfaceArea = info.SurfaceArea;
        _volume = info.Volume;
        _bBox = info.Box;
    }


    public IndexedMesh<TNum> Indexed()
        => this;


    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public static IndexedMesh<TNum> Empty { get; } = new([]);

}