using System.Numerics;

namespace MeshWiz.Math;

public sealed class IndexedMesh<TNum> : IIndexedMesh<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vec3<TNum> Centroid => VolumeCentroid;
    public Vec3<TNum> VertexCentroid => _vertexCentroid ??= Mesh.Math.VertexCentroid(this);
    public Vec3<TNum> SurfaceCentroid => _surfaceCentroid ??= Mesh.Math.SurfaceCentroid(this).XYZ;
    public Vec3<TNum> VolumeCentroid => _volumeCentroid ??= Mesh.Math.VolumeCentroid(this).XYZ;

    public TNum Volume => _volume ??= Mesh.Math.Volume(this);
    public TNum SurfaceArea => _surfaceArea ??= Mesh.Math.SurfaceArea(this);
    public AABB<Vec3<TNum>> BBox => _bBox ??= AABB<Vec3<TNum>>.From(Vertices);

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vec3<TNum>? _vertexCentroid;
    private Vec3<TNum>? _surfaceCentroid;
    private Vec3<TNum>? _volumeCentroid;
    private AABB<Vec3<TNum>>? _bBox;

    public Vec3<TNum>[] Vertices { get; }
    public TriangleIndexer[] Indices { get; }
    public int Count => Indices.Length;
    public Triangle3<TNum> this[int index] => Indices[index].Extract(Vertices);

    public IndexedMesh(Vec3<TNum>[] vertices, TriangleIndexer[] indices)
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

    private static int GetIndex(Vec3<TNum> vec, Dictionary<Vec3<TNum>, int> unified,
        List<Vec3<TNum>> vertices)
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

    public IndexedMesh<TOther> To<TOther>() where TOther : unmanaged, IFloatingPointIeee754<TOther>
    {
        if (typeof(TOther) == typeof(TNum)) return (IndexedMesh<TOther>)(object) this;
        return new(Vertices.Select(v => v.To<TOther>()).ToArray(), Indices);
    }

    public IndexedMesh<TNum> Inverted()
    {
        var indices=Indices.Select(tri => new TriangleIndexer(tri.A, tri.C, tri.B)).ToArray();
        return new(Vertices, indices);
    }
}