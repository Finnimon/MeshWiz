using System.Collections.Generic;
using System.Numerics;

namespace MeshWiz.Math;

public interface IIndexedMesh<TNum> : IMesh<TNum> 
    where TNum : unmanaged, IFloatingPointIeee754<TNum>
{
    public IReadOnlyList<TriangleIndexer> Indices { get; }
    public IReadOnlyList<Vec3<TNum>> Vertices { get; }
}