using System.Numerics;

namespace MeshWiz.Math;

public interface IIndexedMesh<TNum> : IMesh<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public TriangleIndexer[] Indices { get; }
    public Vector3<TNum>[] Vertices { get; }
}