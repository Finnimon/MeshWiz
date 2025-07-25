using System.Collections;
using System.Numerics;

namespace MeshWiz.Math;

public interface IMesh3<TNum> : IReadOnlyList<Triangle3<TNum>>, IBody<TNum>
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public Vector3<TNum> VertexCentroid { get; }
    public Vector3<TNum> SurfaceCentroid { get; }
    public Vector3<TNum> VolumeCentroid { get; }
    Vector3<TNum>  IShape<Vector3<TNum>>.Centroid=> VertexCentroid;
    public IIndexedMesh3<TNum> Indexed()=>new IndexedMesh3<TNum>(this);

    public void InitializeLazies();
    
    IMesh3<TNum> ISurface3<TNum>.Tessellate() => this;
    
    IEnumerator<Triangle3<TNum>> IEnumerable<Triangle3<TNum>>.GetEnumerator()
    {
        for (int i = 0; i < Count; i++) yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}