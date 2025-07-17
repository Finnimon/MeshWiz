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
    Vector3<TNum> IFace<Vector3<TNum>, TNum>.Centroid => SurfaceCentroid;
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
            this.Indices = indexed.Indices;
            this.Vertices = indexed.Vertices;
            return;
        }

        Indices = new TriangleIndexer[mesh.Count];
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
            Indices[i] = new TriangleIndexer(aIndex, bIndex, cIndex);
        }

        Vertices = vertices.ToArray();
    }

    private static uint GetIndex(Vector3<TNum> vec, Dictionary<Vector3<TNum>, uint> unified,
        List<Vector3<TNum>> vertices)
    {
        if (unified.TryGetValue(vec, out var index)) return index;
        index = uint.CreateChecked(vertices.Count);
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

    public void Shift(Vector3<TNum> add)
    {
        for (var i = 0; i < Vertices.Length; i++) Vertices[i] += add;
        InitializeLazies();
    }

    public IndexedMesh3<TNum> Indexed()
        => this;

    public IEnumerator<Triangle3<TNum>> GetEnumerator()
    {
        for (var i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public static IndexedMesh3<TNum> Empty { get; } = new([]);
}