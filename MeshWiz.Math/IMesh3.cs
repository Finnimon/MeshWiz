using System.Numerics;

namespace MeshWiz.Math;

public interface IMesh3<TNum> : IReadOnlyList<Triangle3<TNum>>, IBody<TNum>, IFace<Vector3<TNum>, TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> VertexCentroid { get; }
    public Vector3<TNum> SurfaceCentroid { get; }
    public Vector3<TNum> VolumeCentroid { get; }
    Vector3<TNum> IFace<Vector3<TNum>, TNum>.Centroid => SurfaceCentroid;
    Vector3<TNum> IBody<TNum>.Centroid => VolumeCentroid;

    public IndexedMesh3<TNum> Indexed()=>new(this);
    IFace<Vector3<TNum>, TNum> IBody<TNum>.Surface => this;
    TNum IFace<Vector3<TNum>,TNum>.SurfaceArea => ((IBody<TNum>)this).SurfaceArea;

    public void InitializeLazies();
}