using System.Collections;
using System.Numerics;

namespace MeshWiz.Math;

public sealed record Mesh3<TNum>(Triangle3<TNum>[] TessellatedSurface) : IMesh3<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    
    TNum ISurface<Vector3<TNum>,TNum>.SurfaceArea => SurfaceArea;
    public Vector3<TNum> VertexCentroid => _vertexCentroid ??= MeshMath.VertexCentroid(TessellatedSurface);
    public Vector3<TNum> SurfaceCentroid => _surfaceCentroid ??= MeshMath.SurfaceCentroid(TessellatedSurface).XYZ;
    public Vector3<TNum> VolumeCentroid => _volumeCentroid ??= MeshMath.VolumeCentroid(TessellatedSurface).XYZ;
    public TNum Volume => _volume ??= MeshMath.Volume(TessellatedSurface);


    public TNum SurfaceArea => _surfaceArea ??= MeshMath.SurfaceArea(TessellatedSurface);

    public ISurface<Vector3<TNum>, TNum> Surface => this;
    public BBox3<TNum> BBox =>_bBox??=MeshMath.BBox(TessellatedSurface);

    private TNum? _surfaceArea;
    private TNum? _volume;
    private Vector3<TNum>? _vertexCentroid;
    private Vector3<TNum>? _surfaceCentroid;
    private Vector3<TNum>? _volumeCentroid;
    private BBox3<TNum>? _bBox;

    public IndexedMesh3<TNum> Indexed()=>new(this);
    

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

    public async Task InitializeAsync() => await Task.Run(InitializeLazies);

    public int Count => TessellatedSurface.Length;

    public Triangle3<TNum> this[int index] => TessellatedSurface[index];
}